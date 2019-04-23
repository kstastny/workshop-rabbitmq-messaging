module OrcVillage.Domain

open System

type Orc = {
    Id: Guid
    Name: string
    Profession: string
    Born: DateTime
}

type OrcEvent =
    | Born of Orc