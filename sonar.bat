dotnet test ActiveBC.ProxyBalancer.Tests/ActiveBC.ProxyBalancer.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
dotnet "C:\Program Files\sonar-scanner-msbuild-4.7.1.2311-netcoreapp2.0\SonarScanner.MSBuild.dll" begin /k:"sasinandrei_ActiveBC.ProxyBalancer" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="3a4c69dba8f54c484369213213d7f6a1dcfbdf3a" /o:sasinandrei /d:sonar.cs.opencover.reportsPaths="ActiveBC.ProxyBalancer.Tests\coverage.opencover.xml" /d:sonar.coverage.exclusions="**Test*.cs,**/Program.cs"
dotnet build ActiveBC.ProxyBalancer.sln 
dotnet "C:\Program Files\sonar-scanner-msbuild-4.7.1.2311-netcoreapp2.0\SonarScanner.MSBuild.dll" end /d:sonar.login="3a4c69dba8f54c484369213213d7f6a1dcfbdf3a"