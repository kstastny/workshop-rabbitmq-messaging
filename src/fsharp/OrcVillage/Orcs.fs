module OrcVillage.Orcs

open OrcVillage.Domain
open OrcVillage.Db

let private entityToVehicle (x: DbDataProvider.dataContext.``dbo.OrcsEntity``) : Orc = {
    Id = x.Id
    Name = x.Name
    Profession = x.Profession
    Born = x.Born
}

let addOrc (ctx: DbDataProvider.dataContext) (v: Orc) =
    execute (fun _ ->
        let entity = ctx.Dbo.Orcs.Create()
        entity.Id <- v.Id
        entity.Name <- v.Name
        entity.Profession <- v.Profession
        entity.Born <- v.Born
        
        entity |> entityToVehicle
        )