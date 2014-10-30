namespace FsBeaker

open Owin
open System.Linq
open System.Web.Http
open System.Net.Http.Formatting
open System.Net.Http.Headers
open System
open System.Text
open System.Threading.Tasks
open Newtonsoft.Json
open System.IO

type StringMediaTypeFormatter() = 
    inherit MediaTypeFormatter()

    /// Can only read strings
    override __.CanReadType(t) = 
        t = typeof<string>

    /// Can only write strings
    override __.CanWriteType(t) = 
        t = typeof<string>

    override __.WriteToStreamAsync(t, o, stream, content, context) =
        Task.Factory.StartNew(fun() -> 
            let bytes = Encoding.UTF8.GetBytes(string <| o)
            stream.Write(bytes, 0, bytes.Length)
        )

type JsonContentNegotiator(formatter: JsonMediaTypeFormatter) = 
    interface IContentNegotiator with
        member __.Negotiate(t, req, formatters) = 
            ContentNegotiationResult(formatter, MediaTypeHeaderValue("application/json"))

type Startup() = 

    /// Configures the application to use web api conventions
    member __.Configuration(appBuilder: IAppBuilder) = 
    
        let config = new HttpConfiguration()
        config.Services.Replace(typeof<IContentNegotiator>, new JsonContentNegotiator(new JsonMediaTypeFormatter()));
        config.Routes.MapHttpRoute("FsBeaker", "{controller}/{action}") |> ignore
        appBuilder.UseWebApi(config) |> ignore
