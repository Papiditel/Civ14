﻿using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

[Serializable, NetSerializable]
public sealed partial class MedBookDoAfterEvent : SimpleDoAfterEvent
{
}
