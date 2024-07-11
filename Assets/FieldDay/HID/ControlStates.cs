using System;
using System.Runtime.CompilerServices;
using BeauUtil;

namespace FieldDay.HID {
    /// <summary>
    /// State of a set of digital controls (buttons).
    /// </summary>
    public struct DigitalControlStates {
        public uint Current;
        public uint Prev;

        public uint Pressed;
        public uint Released;

        #region Modifiers

        public void Update(uint current) {
            Prev = Current;
            Current = current;

            uint changes = Current ^ Prev;
            Pressed = changes & Current;
            Released = changes & (~Current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Current = Prev = Pressed = Released = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChanges() {
            Pressed = Released = 0;
        }

        #endregion // Modifiers

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDown(uint mask) {
            return (Current & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDownAll(uint mask) {
            return (Current & mask) == mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly  bool IsPressed(uint mask) {
            return (Pressed & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsReleased(uint mask) {
            return (Released & mask) != 0;
        }

        #endregion // Accessors
    }

    /// <summary>
    /// State of a set of digital controls (buttons), represented by an Enum.
    /// </summary>
    public struct DigitalControlStates<TEnum> where TEnum : unmanaged, Enum {
        public TEnum Current;
        public TEnum Prev;

        public TEnum Pressed;
        public TEnum Released;

        #region Modifiers

        public void Update(TEnum current) {
            Prev = Current;
            Current = current;

            uint changes = Enums.ToUInt(Current) ^ Enums.ToUInt(Prev);
            Pressed = Enums.ToEnum<TEnum>(changes & Enums.ToUInt(Current));
            Released = Enums.ToEnum<TEnum>(changes & ~Enums.ToUInt(Current));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Current = Prev = Pressed = Released = default(TEnum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChanges() {
            Pressed = Released = default(TEnum);
        }

        #endregion // Modifiers

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDown(TEnum mask) {
            return (Enums.ToUInt(Current) & Enums.ToUInt(mask)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsDownAll(TEnum mask) {
            return (Enums.ToUInt(Current) & Enums.ToUInt(mask)) == Enums.ToUInt(mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsPressed(TEnum mask) {
            return (Enums.ToUInt(Pressed) & Enums.ToUInt(mask)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsReleased(TEnum mask) {
            return (Enums.ToUInt(Released) & Enums.ToUInt(mask)) != 0;
        }

        #endregion // Accessors
    }

    /// <summary>
    /// State of an axis.
    /// </summary>
    public struct AxisControlState8 {
        public float Raw;
        public float Adjusted;
        public unsafe fixed float AdjustedHistory[8];
    }
}