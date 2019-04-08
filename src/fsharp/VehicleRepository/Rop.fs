module FSharp.Control.Rop

let bind2 f x y = 
    match x, y with
    | Ok xR, Ok yR -> f xR yR
    | Error e, _ | _, Error e -> Error e

let apply resultF result =
    match resultF with
    | Ok f -> Result.map f result
    | Error e -> Error e

let fold results =
    let foldFn item acc =
        match acc, item with
        | Error e, _ | _, Error e -> Error e
        | Ok l, Ok v -> v :: l |> Ok
    List.foldBack foldFn results (Ok [])

let bindOption e opt =
    match opt with
    | Some(x) -> Ok x
    | None -> Error e

let extractOrNone = function
    | Ok x -> Some x
    | Error _ -> None

let isOk = function
    | Ok(_) -> true
    | _ -> false    

let resultGet x =
    match x with
    | Ok xR -> xR
    | Error e -> failwith <| sprintf "%A" e

let errorGet x =
    match x with
    | Ok _ -> failwith "Cannot get error from Ok case"
    | Error e -> e

let flatten = function
    | Ok tr -> tr
    | Error e -> Error e        


///https://fsharpforfunandprofit.com/posts/elevated-world-4/#traverse (monadic style)
let rec traverse f list = 

    // define the monadic functions
    let (>>=) x f = Result.bind f x
    let retn = Ok

    // loop through the list
    match list with
    | [] -> 
        // if empty, lift [] to a Result
        retn []
    | head::tail ->
        // otherwise lift the head to a Result using f
        // then lift the tail to a Result using traverse
        // then cons the head and tail and return it
        f head                 >>= (fun h -> 
        traverse f tail >>= (fun t ->
        retn (h :: t) ))


type ResultBuilder() = 
    member __.Zero() = Ok()
    member __.Bind(m, f) = Result.bind f m
    member __.Return(x) = Ok x
    member __.ReturnFrom(x) = x
    member __.Combine (a, b) = Result.bind b a
    member __.Delay f = f
    member __.Run f = f ()
    member __.TryWith (body, handler) =
        try
            body()
        with
        | e -> handler e
    member __.TryFinally (body, compensation) =
        try
            body()
        finally
            compensation()
    member x.Using(d:#System.IDisposable, body) =
        let result = fun () -> body d
        x.TryFinally (result, fun () ->
            match d with
            | null -> ()
            | d -> d.Dispose())
    member x.While (guard, body) =
        if not <| guard () then
            x.Zero()
        else
            Result.bind (fun () -> x.While(guard, body)) (body())
    member x.For(s:seq<_>, body) =
        x.Using(s.GetEnumerator(), fun enum ->
            x.While(enum.MoveNext,
                x.Delay(fun () -> body enum.Current)))

let result = ResultBuilder()

let (>>=) result f = Result.bind f result
let (<!>) result f = Result.map f result
let (<*>) = apply

module Async =


    let bind (f:'a -> Async<Result<'b,'c>>) result = 
        async {
            let! res = result
            match res with
            | Ok s -> return! (f s)
            | Error f -> return Error f
        }
    
    let mapError f inp = 
        async {
            let! res = inp
            match res with
            | Ok x -> return Ok x
            | Error e -> return Error (f e)
        }
    
    let apply resultF result =
        async {
            let! resF = resultF
            let! res = result
            return resF <*> res
        }

    let bind2 f x y = 
        async {
            let! xR = x
            let! yR = y
            match xR, yR with
            | Ok xR, Ok yR -> return! f xR yR
            | Error e, _ | _, Error e -> return Error e
        }

    let (>>=) result f = bind f result
    let (<!>) result f = bind (f >> Ok >> async.Return) result
    let (<*>) = apply    