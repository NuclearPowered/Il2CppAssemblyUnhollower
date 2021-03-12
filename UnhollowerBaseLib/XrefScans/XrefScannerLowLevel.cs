using System;
using System.Collections.Generic;
using System.IO;
using Gee.External.Capstone;
using Gee.External.Capstone.X86;

namespace UnhollowerBaseLib.XrefScans
{
    public static class XrefScannerLowLevel
    {
        internal static unsafe X86Instruction[] DecoderForAddress(IntPtr codeStart, int lengthLimit = 1000)
        {
            if (codeStart == IntPtr.Zero) throw new NullReferenceException(nameof(codeStart));

            using var stream = new UnmanagedMemoryStream(((byte*) codeStart)!, lengthLimit, lengthLimit, FileAccess.Read);
            var bytes = new byte[lengthLimit];
            stream.Read(bytes, 0, lengthLimit);

            using var disassembler = CapstoneDisassembler.CreateX86Disassembler(X86DisassembleMode.Bit32);
            disassembler.EnableInstructionDetails = true;

            var decoder = disassembler.Disassemble(bytes, codeStart.ToInt64(), lengthLimit);

            return decoder;
        }

        internal static ulong ExtractTargetAddress(in X86Instruction instruction)
        {
            if (!instruction.HasDetails)
            {
                throw new ArgumentException();
            }

            var operands = instruction.Details.Operands;

            if (operands.Length == 0)
            {
                return 0;
            }

            var operand = operands[0];

            ulong address = 0;

            switch (operand.Type)
            {
                case X86OperandType.Immediate:
                    address = (ulong) operand.Immediate;
                    break;

                case X86OperandType.Memory when operand.Memory.Base == null:
                    address = (ulong) operand.Memory.Displacement;
                    break;

                case X86OperandType.Memory:
                {
                    if (operand.Memory.Base.Id == X86RegisterId.X86_REG_RIP)
                    {
                        address = (ulong) (instruction.Address + operand.Memory.Displacement);
                    }

                    break;
                }
            }

            return address;
        }

        public static IEnumerable<IntPtr> JumpTargets(IntPtr codeStart)
        {
            return JumpTargetsImpl(DecoderForAddress(codeStart));
        }

        private static IEnumerable<IntPtr> JumpTargetsImpl(X86Instruction[] instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.Mnemonic == "ret")
                {
                    yield break;
                }

                if (instruction.Mnemonic == "call")
                {
                    yield return (IntPtr) ExtractTargetAddress(in instruction);
                    // if (instruction.FlowControl == FlowControl.UnconditionalBranch) yield break;
                }
            }
        }
    }
}
