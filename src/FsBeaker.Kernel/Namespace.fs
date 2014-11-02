namespace FsBeaker.Kernel

open System
open System.Net.Http
open System.Text
open FSharp.Data
open Newtonsoft.Json

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type NamespaceBinding = {

    [<JsonProperty("name")>]
    Name: string

    [<JsonProperty("session")>]
    Session: string

    [<JsonProperty("value")>]
    Value: obj

    [<JsonProperty("defined")>]
    Defined: bool
}

/// Allows for setting and getting values from the beaker server
type NamespaceClient(session) =
    
    let password = Environment.GetEnvironmentVariable("beaker_core_password")
    let port = Environment.GetEnvironmentVariable("beaker_core_port")
    let account = "beaker:" + password
    let auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(account))
    let urlBase = "http://127.0.0.1:" + port + "/rest/namespace"

    /// Sets the value
    member __.Set4(name, value, unset, sync) = 
        let jsonValue = JsonConvert.SerializeObject(value)
        let url = String.Format("{0}/set", urlBase)
        let v = if not <| unset then ["value", jsonValue] else []
        Http.RequestString
          ( url, httpMethod = "POST",
            body   = HttpRequestBody.FormValues([ "name", name; "sync", string sync; "session", session ] @ v),
            headers = [ "Authorization", auth ])

    /// Sets the value
    member __.Set(name, value) =
        __.Set4(name, value, false, true)

    /// Sets the value
    member __.SetFast(name, value) =
        __.Set4(name, value, false, true)

    /// Unset the value
    member __.Unset(name) = 
        __.Set4(name, null, true, true)

    // Gets the value as a JSON string
    member __.Get(name) = 
        let url = String.Format("{0}/get", urlBase)
        let json = 
            Http.RequestString
              ( url, httpMethod = "GET",
                query   = [ "name", name; "session", session ],
                headers = [ "Authorization", auth ])

        let binding = JsonConvert.DeserializeObject<NamespaceBinding>(json)
        if not <| binding.Defined then failwithf "name not defined: %s" name
        binding.Value

    /// Operator for getting
    static member (?) (ns:NamespaceClient, name) = 
        ns.Get(name)

    /// Operator for setting data
    static member (?<-) (ns:NamespaceClient, name, value) = 
        ns.Set(name, value)
