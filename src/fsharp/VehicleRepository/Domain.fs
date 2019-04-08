module VehicleRepository.Domain

open System

type Vehicle = {
    Id: Guid
    Name: string
    RegistrationPlate: string
}

type VehicleEvent =
    | VehicleAddedEvent of Vehicle