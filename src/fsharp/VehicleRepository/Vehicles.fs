module VehicleRepository.Vehicles

open VehicleRepository.Domain
open VehicleRepository.Db

let private entityToVehicle (x: DbDataProvider.dataContext.``dbo.VehiclesEntity``) : Vehicle = {
    Id = x.Id
    Name = x.Name
    RegistrationPlate = x.RegistrationPlate
}

let createVehicle (ctx: DbDataProvider.dataContext) (v: Vehicle) =
    execute (fun _ ->
        let entity = ctx.Dbo.Vehicles.Create()
        entity.Id <- v.Id
        entity.Name <- v.Name
        entity.RegistrationPlate <- v.RegistrationPlate
        
        entity |> entityToVehicle
        )