// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System
open System.IO

let project = "FsBeaker"
let summary = "F# implementation for beaker notebook"
let description = "F# implementatino for beaker notebook"
let authors = [ "Peter Rosconi" ]
let tags = "F#, Beaker, Notebook, Data Science"
let solutionFile  = "FsBeaker.sln"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "BayardRock" 
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "FsBeaker"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/BayardRock"

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"

let genFSAssemblyInfo (projectPath) =
    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
    let basePath = "src/" + projectName
    let fileName = basePath + "/AssemblyInfo.fs"
    CreateFSharpAssemblyInfo fileName
      [ Attribute.Title (projectName)
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ]

let genCSAssemblyInfo (projectPath) =
    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
    let basePath = "src/" + projectName + "/Properties"
    let fileName = basePath + "/AssemblyInfo.cs"
    CreateCSharpAssemblyInfo fileName
      [ Attribute.Title (projectName)
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ]

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fsProjs =  !! "src/**/*.fsproj"
  let csProjs = !! "src/**/*.csproj"
  fsProjs |> Seq.iter genFSAssemblyInfo
  csProjs |> Seq.iter genCSAssemblyInfo
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunTests" (fun _ ->
    !! testAssemblies
    |> NUnit (fun p ->
        { p with
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateReferenceDocs" DoNothing

// disable for now    
//Target "GenerateReferenceDocs" (fun _ ->
//    if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:REFERENCE"] [] then
//      failwith "generating reference documentation failed"
//)

let generateHelp fail =
    if executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:HELP"] [] then
        traceImportant "Help generated"
    else
        if fail then
            failwith "generating help documentation failed"
        else
            traceImportant "generating help documentation failed"
    

Target "GenerateHelp" (fun _ ->
    DeleteFile "docs/content/release-notes.md"    
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp true
)


Target "KeepRunning" (fun _ ->    
    use watcher = new FileSystemWatcher(DirectoryInfo("docs/content").FullName,"*.*")
    watcher.EnableRaisingEvents <- true
    watcher.Changed.Add(fun e -> generateHelp false)
    watcher.Created.Add(fun e -> generateHelp false)
    watcher.Renamed.Add(fun e -> generateHelp false)
    watcher.Deleted.Add(fun e -> generateHelp false)

    traceImportant "Waiting for help edits. Press any key to stop."

    System.Console.ReadKey() |> ignore

    watcher.EnableRaisingEvents <- false
    watcher.Dispose()
)

Target "GenerateDocs" DoNothing

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Git.Commit.Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)
//
//Target "Deploy" (fun _ ->
//    !! (buildDir + "/**/*.*") 
//        -- "*.zip" 
//        |> Zip buildDir (deployDir + "Calculator." + version + ".zip")
//)

// Builds a zip file and puts it into the release directory
Target "CreateZip" (fun _ ->

    // create zee working directory
    let workingDir = "release/working"
    workingDir |> CreateDir

    // copy zee binaries to the working directory
    !! ("src/FsBeaker/bin/Release/*.exe")
    ++ ("src/FsBeaker/bin/Release/*.exe.config")
    ++ ("src/FsBeaker/bin/Release/*.dll")
    ++ ("src/FsBeaker/bin/Release/Include.fsx")
    ++ ("src/FsBeaker/bin/Release/FSharp.Core.optdata")
    ++ ("src/FsBeaker/bin/Release/FSharp.Core.sigdata")
    |> CopyFiles(Path.Combine(workingDir, "plugins", "eval", "fsharp", "lib"))

    // copy eval files
    let pluginsDir = Path.Combine(workingDir, "plugins")
    CopyDir pluginsDir "plugins" (fun _ -> true)
    
    // copy other files
    !! ("README.md")
    ++ ("RELEASE_NOTES.md")
    |> CopyFiles(workingDir)

    // finally, zip
    let zipFileName = "release/Release-" + release.SemVer.ToString() + "-alpha.zip"
    !! (workingDir + "/**/*.*")
    |>  Zip workingDir zipFileName

    // cleanup
    workingDir |> CleanDir
    workingDir |> DeleteDir
)

let copyFiles() = 
//    let beakerPluginsDirectory = @"D:\BeakerNotebook1.1\config\plugins"
    let beakerPluginsDirectory = @"C:\BeakerNotebook1.0\config\plugins"
    CopyRecursive "plugins" beakerPluginsDirectory true |> tracefn "%A"

    !! ("src/FsBeaker/bin/Release/*.exe")
    ++ ("src/FsBeaker/bin/Release/*.exe.config")
    ++ ("src/FsBeaker/bin/Release/*.dll")
    ++ ("src/FsBeaker/bin/Release/Include.fsx")
    ++ ("src/FsBeaker/bin/Release/FSharp.Core.optdata")
    ++ ("src/FsBeaker/bin/Release/FSharp.Core.sigdata")
    |> CopyFiles(Path.Combine(beakerPluginsDirectory, "eval", "fsharp", "lib"))

// convenient for testing locally
Target "CopyFiles" copyFiles
Target "CopyFilesOnly" copyFiles

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing
Target "BuildPackage" DoNothing
Target "Release" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "CreateZip"
  =?> ("GenerateReferenceDocs",isLocalBuild && not isMono)
  =?> ("GenerateDocs",isLocalBuild && not isMono)
  ==> "All"
  =?> ("ReleaseDocs",isLocalBuild && not isMono)

"RunTests"
  ==> "CopyFiles"

"All" 
  ==> "BuildPackage"

"CleanDocs"
  ==> "GenerateHelp"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"

"GenerateHelp"
  ==> "KeepRunning"
    
"ReleaseDocs"
  ==> "Release"

RunTargetOrDefault "All"
