# SimElation SimHub plugins.

## Configuring for development

[yarn](https://classic.yarnpkg.com/en/docs/install/#windows-stable) is currently used for package management and release generation.

### Install dependencies

`yarn install` in the root of the repository will install some git hooks to manage commit message style (conventional commits for
semnatic versioning) and code style.

### Development

`yarn develop` will build debug versions of all packages. Requires `msbuild.exe` in the path
([build tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/)) and
[.NET Framework](https://dotnet.microsoft.com/download/visual-studio-sdks).

### Production

`yarn build` will build production versions of all packages.

### Publish a release

`yarn release` will build production versions of all packages and publish to github.
