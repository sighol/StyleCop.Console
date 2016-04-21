// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"

open Fake
open System.IO

let baseDir = __SOURCE_DIRECTORY__
let testReferences = !! "**/*.csproj"
let buildDir = "./build/"

let FindLibraries path =
    let dir = new DirectoryInfo(path)
    dir.GetFiles("*.dll") |> Seq.map (fun x -> path @@ x.Name)


Target "Clean" <| fun _ ->
    CleanDirs [buildDir]


Target "All" <| fun _ ->
    trace "Hello World from FAKE"


Target "Build" <| fun _ ->
    MSBuildDebug buildDir "Build" testReferences
        |> Log "Build-Output: "


Target "Merge" <| fun _ ->
    let dir = new DirectoryInfo(buildDir)
    let libs = FindLibraries buildDir
    ILMerge (fun p -> {p with
                            ToolPath = "packages/ilmerge/tools/ilmerge.exe"
                            TargetKind = Exe
                            Libraries = libs})
            "./StyleCop.exe" (buildDir @@ "StyleCop.Console.exe")


Target "CopyToBin" <| fun _ ->
    for dll in FindLibraries buildDir do
        File.Copy(dll, @"C:\Users\SigHo\bin2\" @@ (new FileInfo(dll)).Name, true)

    File.Copy(buildDir @@ "StyleCop.Console.exe", @"C:\Users\SigHo\bin2\" @@ "StyleCop.exe", true)
    File.Copy(buildDir @@ "Settings.StyleCop", @"C:\Users\SigHo\bin2\" @@ "Settings.StyleCop", true)



"Clean"
    ==> "Build"
//    ==> "Merge"
//    ==> "CopyToBin"
    ==> "All"


RunTargetOrDefault "All"
