// include Fake libs
#I @"tools\FAKE\tools\"
#r @"tools\FAKE\tools\FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.Git
open System.IO

let projectName          = "PROJECT" 

// Directories
let buildDir             = @".\build"

let webBuildDir          = buildDir + @"\web"
let appBuildDir          = buildDir + @"\app"
let PluginbuildDir       = buildDir + @"\plugin"
let testDir              = buildDir + @".\test"

let deployDir            = @".\Publish"

let packagesDir             = @".\packages\"

// version info
let mutable version         = "1.0"
let mutable build           = buildVersion 
let mutable nugetVersion    = ""
let mutable asmVersion      = ""
let mutable asmInfoVersion  = ""
let mutable setupVersion    = ""

let gitbranch = Git.Information.getBranchName "."
let sha = Git.Information.getCurrentHash() 

// Targets
Target "Clean" (fun _ -> 

    CleanDirs [buildDir; deployDir; testDir]
)

Target "RestorePackages" (fun _ ->

   let RestorePackages2() = 
     !! "./**/packages.config" 
     |> Seq.iter ( RestorePackage (fun p -> {p with Sources = ["http://build:8080/api/"; "https://nuget.org/api/v2/"] } ))
     ()
    
   RestorePackages2() 
)

Target "BuildVersions" (fun _ ->

    let safeBuildNumber = if not isLocalBuild then build else "0"

    asmVersion      <- version + "." + safeBuildNumber
    asmInfoVersion  <- asmVersion + " - " + gitbranch + " - " + sha

    nugetVersion    <- version + "." + safeBuildNumber
    setupVersion    <- version + "." + safeBuildNumber

    match gitbranch with
        | "master" -> ()
        | "develop" -> (nugetVersion <- nugetVersion + "-" + "beta")
        | _ -> (nugetVersion <- nugetVersion + "-" + gitbranch)
    
    SetBuildNumber nugetVersion   // Publish version to TeamCity
)

Target "AssemblyInfo" (fun _ ->
    BulkReplaceAssemblyInfoVersions "src/" (fun f -> 
                                              {f with
                                                  AssemblyVersion = asmVersion
                                                  AssemblyInformationalVersion = asmInfoVersion})
)

Target "BuildWeb" (fun _ ->
    !! @"src\web\*.csproj"
      |> MSBuildRelease webBuildDir "Build"
      |> Log "Build-Output: "
)

Target "BuildApp" (fun _ ->
    !! @"src\app\*.csproj"
      |> MSBuildRelease appBuildDir "Build"
      |> Log "Build-Output: "
)

Target "BuildPlugin" (fun _ ->
    !! @"src\plugin\*.csproj"
      |> MSBuildRelease pluginBuildDir "Build"
      |> Log "Build-Output: "
)

Target "BuildTest" (fun _ ->
    !! @"src\test\*.csproj"
      |> MSBuildRelease testDir "Build"
      |> Log "Build-Output: "
)

Target "NUnitTest" (fun _ ->  
    if (Directory.GetFiles(testDir).Length <> 0) then           //NUnit will fail build when there is nothing to test
        !! (testDir + @"\*.Tests.dll")
            |> NUnit (fun p -> 
                {p with 
                    ToolPath = @".\tools\NUnit.Runners\tools\"; 
                    Framework = "net-4.0";
                    DisableShadowCopy = true; 
                    OutputFile = testDir + @"\TestResults.xml"})
)

Target "Zip" (fun _ ->
    !! (buildDir + "/**/*.*") 
        -- "*.zip" 
        |> Zip buildDir (deployDir + projectName + version + ".zip")

// Dependencies
"Clean"
  ==> "RestorePackages"
  ==> "BuildVersions"
  =?> ("AssemblyInfo", not isLocalBuild ) 
  ==> "BuildWeb"
  ==> "BuildTest"
  ==> "BuildPlugin"
  ==> "BuildApp"
  ==> "NUnitTest" 
  ==> "Zip"

 
// start build
RunTargetOrDefault "Publish"