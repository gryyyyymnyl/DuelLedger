using System;

using DuelLedger.Core.Abstractions;
namespace DuelLedger.UI.Models;

public static class Mappers
{
    private static TEnum SafeEnum<TEnum>(int value, TEnum fallback) where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value) ? (TEnum)Enum.ToObject(typeof(TEnum), value) : fallback;
    }

    public static MatchRecord ToDomain(this MatchSummaryDto dto)
    {
        return new MatchRecord
        {
            Format = SafeEnum(dto.Format, MatchFormat.Unknown),
            SelfClass = SafeEnum(dto.SelfClass, PlayerClass.Unknown),
            OppClass = SafeEnum(dto.OppClass, PlayerClass.Unknown),
            Order = SafeEnum(dto.Order, TurnOrder.Unknown),
            Result = SafeEnum(dto.Result, MatchResult.Unknown),
            StartedAt = dto.StartedAt,
            EndedAt = dto.EndedAt,
        };
    }
}