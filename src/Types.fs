namespace Thoth.Json.Net

open System.Text.Json.Nodes
open System.Text.RegularExpressions
open System.Text.Json

type ErrorReason =
    | BadPrimitive of string * JsonElement
    | BadPrimitiveExtra of string * JsonElement * string
    | BadType of string * JsonElement
    | BadField of string * JsonElement
    | BadPath of string * JsonElement * string
    | TooSmallArray of string * JsonElement
    | FailMessage of string
    | BadOneOf of string list

type CaseStrategy =
    | PascalCase
    | CamelCase
    | SnakeCase

type DecoderError = string * ErrorReason

type Decoder<'T> = string -> JsonElement -> Result<'T, DecoderError>

type Encoder<'T> = 'T -> JsonNode

[<AbstractClass>]
type BoxedDecoder() =
    abstract Decode: path : string * token: JsonElement -> Result<obj, DecoderError>
    member this.BoxedDecoder: Decoder<obj> =
        fun path token -> this.Decode(path, token)

[<AbstractClass>]
type BoxedEncoder() =
    abstract Encode: value: obj -> JsonNode
    member this.BoxedEncoder: Encoder<obj> = this.Encode

type ExtraCoders =
    { Hash: string
      Coders: Map<string, BoxedEncoder * BoxedDecoder> }

module internal Cache =
    open System.Collections.Concurrent

    type Cache<'Value>() =
        let cache = ConcurrentDictionary<string, 'Value>()
        member _.GetOrAdd(key: string, factory: string->'Value) =
            cache.GetOrAdd(key, factory)

    let Encoders = lazy Cache<BoxedEncoder>()
    let Decoders = lazy Cache<BoxedDecoder>()

module internal Util =

    module Casing =
        let lowerFirst (str: string) = str[..0].ToLowerInvariant() + str[1..]
        let convert caseStrategy fieldName =
            match caseStrategy with
            | CamelCase -> lowerFirst fieldName
            | SnakeCase -> Regex.Replace(lowerFirst fieldName, "[A-Z]","_$0").ToLowerInvariant()
            | PascalCase -> fieldName
