module OrcVillage.OrcMother

open System

open OrcVillage.Domain

let private rnd = new Random()

//random google for Orc names came up with this https://en.uesp.net/wiki/Lore:Orc_Names
let private names = [|
            "Moghakh"; "Atulg"; "Azuk"; "Bagamul"; "Bashag"; "Bologra"; "Borug";
            "Dur"; "Dular"; "Duma"; "Garothmuk"; "Garzonk"; "Ghoragdush"; "Khagra";
            "Lorzub"; "Lugrub"; "Olumba"; "Orakh"
            |]


module Array =

    let random (x: 'a []) =
        match x.Length with
        | 0 -> failwith "Cannot select from nonempty array"
        | _ -> Array.get x (rnd.Next x.Length)


let giveBirth(): Orc = {
    Id = Guid.NewGuid()
    Name = names |> Array.random
    // All orcs are warriors
    Profession = "Warrior"
    Born = DateTime.Now
 }





