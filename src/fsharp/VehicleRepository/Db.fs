module VehicleRepository.Db

open System
open System.Data.SqlClient

open FSharp.Data.Sql

type DataError =
    | General of Exception
    | SqlServerNotAvailable of Exception
    | DuplicateKey of message:string
    | ResourceNotFound of id:string
    | IdNotUnique of id:string
    with
        member x.Explain() =
            match x with
            | General ex -> sprintf "An unexpected exception occured: %s." ex.Message
            | SqlServerNotAvailable ex -> sprintf "Cannot connect to SQL server: %A" ex.Message
            | DuplicateKey m -> sprintf "Duplicate key, operation cannot proceed: %s" m
            | ResourceNotFound id -> sprintf "Resource with the id: '%s' was not found." id
            | IdNotUnique id -> sprintf "The supplied id: '%s' is not unique." id

[<Literal>]
let private DbConnection =
    """Server=(LocalDB)\messaging;Initial Catalog=messaging_samples;Persist Security Info=False;Integrated security=False;User ID=messaging;Password=Vo60&8cV7erE;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"""


type DbDataProvider =
        SqlDataProvider<
                DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER,
                UseOptionTypes = true,
                ConnectionString = DbConnection
                >
                
let createContext (connectionString: string) = DbDataProvider.GetDataContext connectionString                
                
[<Literal>]
let private DuplicateKeyError = 2627

[<Literal>]
let private SqlServerNotAvailable = 10061


let executeWithParam body param =
    try
        param |> body |> Ok
    with
        | :? SqlException as ex ->
                match ex.Number with
                | DuplicateKeyError -> DataError.DuplicateKey ex.Message |> Error
                | SqlServerNotAvailable -> DataError.SqlServerNotAvailable ex |> Error
                | _ -> DataError.General ex |> Error
        | ex -> DataError.General ex |> Error

let executeResult f =
    try
        f ()
    with
    | :? SqlException as ex ->
            match ex.Number with
            | DuplicateKeyError -> DataError.DuplicateKey ex.Message |> Error
            | SqlServerNotAvailable -> DataError.SqlServerNotAvailable ex |> Error
            | _ -> DataError.General ex |> Error
    | ex -> DataError.General ex |> Error


let execute f = executeResult (fun _ -> f () |> Ok)

let exists<'a> (q:seq<'a>) =
    execute (fun _ ->
        q |> Seq.length > 0
    )

let exactlyOneOrError id s =
    match Seq.toList s with
    | [h] -> Ok h
    | [] -> ResourceNotFound id |> Error
    | _ -> IdNotUnique id |> Error

let exactlyOne<'a> id (q:seq<'a>) =
    executeResult (fun _ ->
        q |> exactlyOneOrError id
    )                