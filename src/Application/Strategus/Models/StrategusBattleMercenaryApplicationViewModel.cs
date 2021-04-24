﻿using Crpg.Application.Characters.Models;
using Crpg.Application.Users.Models;
using Crpg.Domain.Entities.Strategus.Battles;

namespace Crpg.Application.Strategus.Models
{
    public record StrategusBattleMercenaryApplicationViewModel
    {
        public int Id { get; init; }
        public UserPublicViewModel User { get; init; } = default!;
        public CharacterPublicViewModel Character { get; init; } = default!;
        public StrategusBattleMercenaryApplicationStatus Status { get; init; }
    }
}
