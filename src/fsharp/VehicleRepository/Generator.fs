module VehicleRepository.Generator

open System

open VehicleRepository.Domain

let private rnd = new Random()

let private names = [| "Škoda"; "VW"; "Opel"; "Renault"; "Peugeot";  |]

let regPlateLetters = [| 'A'; 'B'; 'C'; 'E'; 'H'; 'J'; 'K'; 'L'; 'M'; 'P'; 'S'; 'T'; 'U'; 'Z' |]


module Array =
    
    let random (x: 'a[]) =
        match x.Length with
        | 0 -> failwith "Cannot select from nonempty array"
        | _ -> Array.get x (rnd.Next x.Length)
        

let generateVehicle () : Vehicle = {
    Id = Guid.NewGuid ()
    Name = sprintf "%s - %i" (names |> Array.random) (rnd.Next 999)
    RegistrationPlate =
        sprintf "%i%O%i %i"
            (rnd.Next 10)
            (regPlateLetters |> Array.random)
            (rnd.Next 10)
            (rnd.Next (1000,9999))
}

    
    
    

