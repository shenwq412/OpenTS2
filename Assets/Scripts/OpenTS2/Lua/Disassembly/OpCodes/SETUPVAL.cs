﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTS2.Lua.Disassembly.OpCodes
{
    public class SETUPVAL : LuaC50.OpCode
    {
        public override void Disassemble(LuaC50.Context context)
        {
            context.Code.WriteLine(context.U(B)+" = "+context.R(A));
        }
    }
}
