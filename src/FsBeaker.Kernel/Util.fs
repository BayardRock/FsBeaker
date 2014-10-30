namespace FsBeaker.Kernel

open System
open System.Drawing
open System.IO
open System.Net
open System.Text
open System.Web
open System.Drawing.Imaging
open System.Windows.Forms
open FSharp.Charting
open Newtonsoft.Json

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type BinaryOutput =
    { 
        ContentType: string;
        Data: obj
    }

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type TableOutput = 
    {
        Columns: array<string>;
        Rows: array<array<string>>;
    }

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type LatexOutput =
    {
        Latex: string;
    }

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type HtmlOutput =
    {
        Html: string;
    }

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type GenericChartWithSize = 
    {
        Chart: ChartTypes.GenericChart;
        Size: int * int;
    }

[<AutoOpen>]
module ExtensionMethodsAndGlobals =
    open System.Reflection

    /// Looks at the object passed in and provies some help
    let rec Help (item:obj) =

        match item with
        | :? Type as t ->

            let isOption (t: Type) = 
                t.IsGenericType &&
                t.GetGenericTypeDefinition() = typedefof<Option<_>>

            let formatParameter (pi: ParameterInfo) =
                if isOption(pi.ParameterType) then
                    let innerType = pi.ParameterType.GetGenericArguments().[0]
                    "?" + pi.Name + " : " + innerType.ToString()
                else
                    pi.ParameterType.ToString()
                
            let formatParameters (pi: ParameterInfo[]) =
                String.Join(", ", pi |> Seq.map formatParameter)

            let propText =
                t.GetProperties()
                |> Seq.map (fun x -> x.Name + " : " + x.PropertyType.Name)
                |> Seq.sort

            let methText = 
                t.GetMethods()
                |> Seq.filter (fun x -> not <| x.Name.StartsWith("get_"))
                |> Seq.filter (fun x -> not <| x.Name.StartsWith("set_"))
                |> Seq.map (fun x -> x.Name + "(" + formatParameters(x.GetParameters()) + ")")
                |> Seq.sort

            let consText =
                t.GetConstructors()
                |> Seq.map (fun x -> x.Name + "(" + formatParameters(x.GetParameters()) + ")")
                |> Seq.sort

            let formatNames (sb:StringBuilder) (header:string) (s:seq<_>) =
                let items = s |> Seq.toArray
                if items.Length > 0 then
                    let separator = "".PadLeft(header.Length, '=')
                    sb.AppendLine(header)
                        .AppendLine(separator)
                        .AppendLine(String.Join("\n", s))
                        .AppendLine()
                        .ToString()
                else
                    ""

            let sb = StringBuilder()
            consText |> formatNames sb "Constructors" |> ignore
            propText |> formatNames sb "Properties" |> ignore
            methText |> formatNames sb "Methods" |> ignore
            sb.ToString()

        | _ -> item.GetType() |> Help

    type Exception with
        
        /// Convenience method for getting the full stack trace by going down the inner exceptions
        member self.CompleteStackTrace() = 
            
            let mutable ex = self
            let sb = StringBuilder()
            while ex <> null do
                sb.Append(ex.GetType().Name)
                  .AppendLine(ex.Message)
                  .AppendLine(ex.StackTrace) |> ignore

                ex <- ex.InnerException

            sb.ToString()

    type ChartTypes.GenericChart with 

        /// Wraps a GenericChartWithSize around the GenericChart
        member self.WithSize(x:int, y:int) =
            {
                Chart = self;
                Size = (x, y);
            }

        /// Converts the GenericChart to a PNG, in order to do this, we must show a form with ChartControl on it, save the bmp, then write the png to memory
        member self.ToPng(?size) =

            // get the size
            let (width, height) = if size.IsNone then (320, 240) else size.Value

            // create a new ChartControl in order to get the underlying Chart
            let ctl = new ChartTypes.ChartControl(self)

            // save
            use ms = new MemoryStream()
            let actualChart = ctl.Controls.[0] :?> System.Windows.Forms.DataVisualization.Charting.Chart
            actualChart.Dock <- DockStyle.None
            actualChart.Size <- Size(width, height)
            actualChart.SaveImage(ms, ImageFormat.Png)
            ms.ToArray()

    type FSharp.Charting.Chart with
    
        /// Wraps a GenericChartWithSize around the GenericChart
        static member WithSize(x:int, y:int) = 

            fun (ch : #ChartTypes.GenericChart) ->
                ch.WithSize(x, y)

type Util = 

    /// Wraps a LatexOutput around a string in order to send to the UI.
    static member Latex (str) =
        { Latex = str}

    /// Wraps a LatexOutput around a string in order to send to the UI.
    static member Math (str) =
        { Latex = "$$" + str + "$$" }

    /// Wraps a HtmlOutput around a string in order to send to the UI.
    static member Html (str) =
        { Html = str }

    ///  Creates an array of strings with the specified properties and the item to get the values out of.
    static member Row (columns:seq<Reflection.PropertyInfo>) (item:'A) =
        columns
        |> Seq.map (fun p -> p.GetValue(item))
        |> Seq.map (sprintf "%A")
        |> Seq.toArray

    /// Creates a TableOutput out of a sequence of items and a list of property names.
    static member Table (items:seq<'A>, ?propertyNames:seq<string>) =

        // get the properties
        let properties =
            if propertyNames.IsSome then
                typeof<'A>.GetProperties()
                |> Seq.filter (fun x -> (propertyNames.Value |> Seq.exists (fun y -> x.Name = y)))
                |> Seq.toArray
            else
                typeof<'A>.GetProperties()

        {
            Columns = properties |> Array.map (fun x -> x.Name);
            Rows = items |> Seq.map (Util.Row properties) |> Seq.toArray;
        }

    /// Downloads the specified url and wraps a BinaryOutput around the results.
    static member Url (url:string) =
        let req = WebRequest.Create(url)
        let res = req.GetResponse()
        use stream = res.GetResponseStream()
        use mstream = new MemoryStream()
        stream.CopyTo(mstream)
        { ContentType = res.ContentType; Data =  mstream.ToArray() }

    /// Wraps a BinaryOutput around image bytes with the specified content-type
    static member Image (bytes:seq<byte>, ?contentType:string) =
        {
            ContentType = if contentType.IsSome then contentType.Value else "image/jpeg";
            Data = bytes;
        }

    /// Loads a local image from disk and wraps a BinaryOutput around the image data.
    static member Image (fileName:string) =
        Util.Image (File.ReadAllBytes(fileName))
