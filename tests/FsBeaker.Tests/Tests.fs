namespace FsBeaker.Tests

open NUnit.Framework
open FsBeaker
open FsBeaker.Kernel

[<TestFixture>]
type TestClass() = 

    [<Test>]
    member __.TestKernel() = 
    
        use client = ConsoleKernelClient.StartNewProcess()
        let code = """[1..100]
|> Seq.map float
|> Seq.map (fun x -> x, x)
|> Chart.Line

    """
        let result = client.Execute(code)
        Assert.NotNull(result)
        Assert.AreEqual("image/png", result.Result.ContentType)

        let autoComplete = client.Autocomplete(code, 17)
        Assert.NotNull(autoComplete)
        Assert.AreEqual(68, autoComplete.Declarations.Length)

        let autoComplete2 = client.Autocomplete(code, 18)
        Assert.NotNull(autoComplete2)
        Assert.AreEqual(7, autoComplete2.Declarations.Length)
        Assert.AreEqual(7, autoComplete2.Declarations |> Seq.filter (fun x -> x.StartsWith("m")) |> Seq.length)

