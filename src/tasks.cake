#load "cache.cake"

const string projectName = "OctopusTest";
string dockerBuildImageName = projectName.ToLowerInvariant();
ImageTag dockerBuildTag = new ImageTag("build");

string hostProject = $"{projectName}.Host";
string hostOctopusProject = $"{hostProject}";
TaskArg<FilePath> hostProjectArg = new TaskArg<FilePath>((ICakeContext ctx) => ctx.File($"{hostProject}/{hostProject}.csproj"));

TaskArg<FilePath> solutionArg = new TaskArg<FilePath>((ICakeContext ctx) => ctx.File($"{projectName}.sln"));

TaskArg<DirectoryPath> dockerContextArg = new TaskArg<DirectoryPath>((ICakeContext ctx) => ctx.Environment.WorkingDirectory);
TaskArg<FilePath> dockerFileArg = new TaskArg<FilePath>((ICakeContext ctx) => ctx.File($"Dockerfile"));

TaskArg<IEnumerable<FilePath>> testProjectsArg = new TaskArg<IEnumerable<FilePath>>((ICakeContext ctx) => ctx.GetFiles("Tests/**/*.csproj"));

ScriptData buildData;

Setup(setupContext => {
    buildData = setupContext.Data.Get<ScriptData>();
});

var clean = CommonTasks.CleanArtifacts();
var dotnetClean = DoNetTasks.DotNetClean(solutionArg);
var collectDockerArtifacts = DockerTasks.CollectBuildArtifacts(dockerBuildImageName, dockerBuildTag.Tag);

var dockerBuild = FUNCTION((string cakeTarget) => DockerTasks.BuildWithCake(dockerContextArg, dockerFileArg, ctx => new DockerImageBuildOptions(dockerBuildImageName, new [] { dockerBuildTag }, target: "build"), cakeTarget));
var codeAnalysys = dockerBuild(KnownTasks.DOCKER.ANALYSIS);
var buildArtifacts = dockerBuild(KnownTasks.DOCKER.BUILD_ARTIFACTS);

Task(KnownTasks.DEFAULT)
    .IsDependentOn(clean)
    .IsDependentOn(buildArtifacts)
    .IsDependentOn(collectDockerArtifacts)
    .IsDependentOn(DockerTasks.DockerBuildServiceImage(dockerContextArg, dockerFileArg, hostProjectArg, "host", buildArtifacts))
    ;

Task(KnownTasks.CI.BUILD)
    .IsDependentOn(KnownTasks.DEFAULT)
    .IsDependentOn(NuGetTasks.PushPackages())
    .IsDependentOn(DockerTasks.DockerPushImages())
    .IsDependentOn(OctopusTasks.OctoPushPackages())
    .IsDependentOn(OctopusTasks.OctoCreateRelease(hostProjectArg, hostOctopusProject))
    ;

Task(KnownTasks.CI.ANALYSIS)
    .IsDependentOn(clean)
    .IsDependentOn(codeAnalysys)
    .IsDependentOn(collectDockerArtifacts);

Task(KnownTasks.DOCKER.ANALYSIS)
    .IsDependentOn(DoNetTasks.DotNetRestore(solutionArg))
    .IsDependentOn(SonarQubeTasks.SonarQubeBegin())
    .IsDependentOn(DoNetTasks.DotNetBuild(solutionArg))
    .IsDependentOn(UnitTestsTasks.RunUnitTests(testProjectsArg))
    .IsDependentOn(SonarQubeTasks.SonarQubeEnd());

Task(KnownTasks.DOCKER.BUILD_ARTIFACTS)
    .IsDependentOn(KnownTasks.DOCKER.ANALYSIS)
    .IsDependentOn(DoNetTasks.DotNetPublish(hostProjectArg, ctx => ctx.Directory($"{hostProject}/bin/publish")))
    .IsDependentOn(NuGetTasks.CollectPackagesToPush(ctx => ctx.GetFiles($"**/bin/{buildData.Configuration}/*.nupkg")))
    .IsDependentOn(OctopusTasks.OctoPackProjects(ctx => ctx.GetFiles("**/*.csproj")));

Task(KnownTasks.CLEAN)
    .IsDependentOn(dotnetClean)
    .IsDependentOn(clean);

Task("Restore")
    .IsDependentOn(DoNetTasks.DotNetRestore(solutionArg));

Task(KnownTasks.BUILD)
    .IsDependentOn(DoNetTasks.DotNetRestore(solutionArg))
    .IsDependentOn(DoNetTasks.DotNetBuild(solutionArg))
    .IsDependentOn(DoNetTasks.DotNetPublish(hostProjectArg, ctx => ctx.Directory($"{hostProject}/bin/publish")))
    .IsDependentOn(NuGetTasks.CollectPackagesToPush(ctx => ctx.GetFiles($"**/bin/{buildData.Configuration}/*.nupkg")))
    .IsDependentOn(OctopusTasks.OctoPackProjects(ctx => ctx.GetFiles("**/*.csproj")));

Task(KnownTasks.REBUILD)
    .IsDependentOn(KnownTasks.CLEAN)
    .IsDependentOn(KnownTasks.BUILD);

Task(KnownTasks.TEST)
    .IsDependentOn(KnownTasks.BUILD)
    .IsDependentOn(UnitTestsTasks.RunUnitTests(testProjectsArg));

Task("DeployApi")
    .IsDependentOn(OctopusTasks.OctoDeployRelease(hostProjectArg, hostOctopusProject));
