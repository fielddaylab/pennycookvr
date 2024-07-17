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

        public bool Update(uint current) {
            Prev = Current;
            Current = current;

            uint changes = Current ^ Prev;
            Pressed = changes & Current;
            Released = changes & (~Current);
            return changes != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Current = Prev = Pressed = Released = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChanges() {
            Pressed = Released = 0;
        }

        /// <summary>
        /// Consumes a press.
        /// Returns if it was pressed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumePress(uint mask) {
            bool had = (Pressed & mask) != 0;
            Pressed &= ~mask;
            return had;
        }

        /// <summary>
        /// Consumes a release.
        /// Returns if it was released.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumeRelease(uint mask) {
            bool had = (Released & mask) != 0;
            Released &= ~mask;
            return had;
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

        public bool Update(TEnum current) {
            Prev = Current;
            Current = current;

            uint changes = Enums.ToUInt(Current) ^ Enums.ToUInt(Prev);
            Pressed = Enums.ToEnum<TEnum>(changes & Enums.ToUInt(Current));
            Released = Enums.ToEnum<TEnum>(changes & ~Enums.ToUInt(Current));
            return changes != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Current = Prev = Pressed = Released = default(TEnum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChanges() {
            Pressed = Released = default(TEnum);
        }

        /// <summary>
        /// Consumes a press.
        /// Returns if it was pressed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumePress(TEnum mask) {
            uint pressedInt = Enums.ToUInt(Pressed);
            uint maskInt = Enums.ToUInt(mask);
            bool had = (pressedInt & maskInt) != 0;
            Pressed = Enums.ToEnum<TEnum>(pressedInt & ~maskInt);
            return had;
        }

        /// <summary>
        /// Consumes a release.
        /// Returns if it was released.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConsumeRelease(TEnum mask) {
            uint releasedInt = Enums.ToUInt(Released);
            uint maskInt = Enums.ToUInt(mask);
            bool had = (releasedInt & maskInt) != 0;
            Released = Enums.ToEnum<TEnum>(releasedInt & ~maskInt);
            return had;
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