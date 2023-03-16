namespace Thoth.Json.Net

[<AutoOpen>]
module internal Helpers =

    let getTypeName (t : System.Type) : TypeName = (t.GetGenericTypeDefinition ()).FullName |> TypeName
