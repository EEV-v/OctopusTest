ARG REGISTRY
ARG BASEIMAGE=gotrg/dotnet
ARG SDK=2.1.403
ARG RUNTIME=2.1.5

FROM ${REGISTRY}/${BASEIMAGE}:${SDK}-sdk AS sdk
WORKDIR /src

FROM ${REGISTRY}/${BASEIMAGE}:${RUNTIME}-aspnetcore-runtime AS asp-runtime
WORKDIR /app
EXPOSE 5000

FROM ${REGISTRY}/${BASEIMAGE}:${RUNTIME}-runtime AS runtime
WORKDIR /app

FROM sdk AS global-cake-cache
ARG CAKE_NUGETFEEDS
COPY --chown=app:app build.ps1 .
COPY --chown=app:app cake.config .
COPY --chown=app:app cake.version .
COPY --chown=app:app build.cake .
COPY --chown=app:app cache.cake .
RUN pwsh -File build.ps1 -Script cache.cake -Target Cache -NuGetFeeds "${CAKE_NUGETFEEDS}"
RUN chmod ug+x -R /src/tools

FROM global-cake-cache as cake-cache
COPY --chown=app:app tasks.cake .

FROM cake-cache as dotnet-cache
ARG CAKE_CONFIGURATION=Release
ARG CAKE_VERBOSITY=Normal
ARG CAKE_NUGETFEEDS
ARG CAKE_DOCKERSOURCE
ARG CAKE_ARGUMENTS
COPY --chown=app:app global.json .
COPY --chown=app:app .editorconfig .
COPY --chown=app:app OctopusTest.sln.DotSettings .
COPY --chown=app:app OctopusTest.sln .
COPY --chown=app:app OctopusTest.Api/OctopusTest.Api.csproj OctopusTest.Api/
COPY --chown=app:app OctopusTest.Api.DI/OctopusTest.Api.DI.csproj OctopusTest.Api.DI/
COPY --chown=app:app OctopusTest.Host/OctopusTest.Host.csproj OctopusTest.Host/
COPY --chown=app:app Tests/OctopusTest.Api.Tests/OctopusTest.Api.Tests.csproj Tests/OctopusTest.Api.Tests/
COPY --chown=app:app Tests/OctopusTest.Host.Tests/OctopusTest.Host.Tests.csproj Tests/OctopusTest.Host.Tests/
COPY --chown=app:app OctopusTest.BusinessLogic/OctopusTest.BusinessLogic.csproj OctopusTest.BusinessLogic/
COPY --chown=app:app OctopusTest.BusinessLogic.Contract/OctopusTest.BusinessLogic.Contract.csproj OctopusTest.BusinessLogic.Contract/
COPY --chown=app:app OctopusTest.BusinessLogic.DI/OctopusTest.BusinessLogic.DI.csproj OctopusTest.BusinessLogic.DI/
COPY --chown=app:app Tests/OctopusTest.BusinessLogic.Tests/OctopusTest.BusinessLogic.Tests.csproj Tests/OctopusTest.BusinessLogic.Tests/
RUN pwsh -File build.ps1 -Target Restore -Configuration ${CAKE_CONFIGURATION} -Verbosity ${CAKE_VERBOSITY} -NuGetFeeds "${CAKE_NUGETFEEDS}" -DockerSource "${CAKE_DOCKERSOURCE}" ${CAKE_ARGUMENTS}

FROM dotnet-cache AS build
ARG CAKE_TARGET=Build
ARG CAKE_CONFIGURATION=Release
ARG CAKE_VERBOSITY=Normal
ARG CAKE_NUGETFEEDS
ARG CAKE_DOCKERSOURCE
ARG CAKE_ARGUMENTS
ARG TEAMCITY_VERSION
ARG TEAMCITY_BUILDCONF_NAME
ARG BUILD_NUMBER
ARG TEAMCITY_PROJECT_NAME
ARG Git_Branch
ENV TEAMCITY_VERSION=${TEAMCITY_VERSION}
ENV TEAMCITY_BUILDCONF_NAME=${TEAMCITY_BUILDCONF_NAME}
ENV BUILD_NUMBER=${BUILD_NUMBER}
ENV TEAMCITY_PROJECT_NAME=${TEAMCITY_PROJECT_NAME}
ENV Git_Branch=${Git_Branch}
COPY --chown=app:app . .
RUN pwsh -File build.ps1 -Target ${CAKE_TARGET} -Configuration ${CAKE_CONFIGURATION} -Verbosity ${CAKE_VERBOSITY} -NuGetFeeds "${CAKE_NUGETFEEDS}" -DockerSource "${CAKE_DOCKERSOURCE}" ${CAKE_ARGUMENTS}

FROM build AS publish-host
WORKDIR /src/OctopusTest.Host
RUN cp -r bin/publish/* /publish



FROM asp-runtime AS host
COPY --chown=app:app --from=publish-host /publish .
ENTRYPOINT ["dotnet", "bundle/OctopusTest.Host.dll"]


