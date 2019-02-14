#load "nuget:?package=R1.Cake.Scripts.Deploy&version=1.6.9&include=scripts/*.cake"

Task("Cache");
Task("Initialize")
    .Does(ctx => {
        var buildConfig = BuildConfig.Init();
        var deployConfig = DeployConfig.Init();

        var name = GetUniqueName(buildConfig, deployConfig);

        if (IsRunningOnWindows())
        {
            ReconfigureWindowsService(ctx, name, buildConfig, deployConfig);
        }
        else
        {
            var options = new RunDockerContainerOptions();
            ReconfigureDockerContainer(ctx, name, buildConfig, deployConfig, options);
        }
    });

RunTarget(Argument("target", "Initialize"));