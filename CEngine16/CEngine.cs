using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace CEngine16
{
    public class CEngine
    {   // Form1.load calls ipl mem contains micro cwds, Cvbl has type name and value
        public void ipl(ListBox lBin, List<uint> mem, List<Parser.Cvbl> cVin)
        {
            AdrCtl aC = new AdrCtl();   // AdrCtl generates memory block addresses for exe
            aC.vbls = new VRam();   // Type values used in exe
            foreach (Parser.Cvbl v in cVin)
                aC.vbls.vbls.Add(v.val);    // may need to add non int types
            aC.cV = cVin;
            mem.Add(0); // nop pad to prevent out of range exception -- Unnecessary???
            aC.sCwA = new CtlRom(mem);  // maybe unnecessary if mem is used
            aC.sCwA.srom.Capacity = 256;
            aC.sCwA.srom.AddRange(mem.ToArray());   // makes chip memory size a power of two for build

            aC.sCwA.srom.Add(0);    // nop padding to prevent out of range exception  -- Unnecessary???
            aC.sCwA.srom.Add(0);
            aC.vbls.vbls.Capacity = 256;    // need to calculate for stack size
            while (aC.vbls.vbls.Count < aC.vbls.vbls.Capacity)
                aC.vbls.vbls.Add(0);
            aC.Alu.ac = aC; // in case Alu needs an aC field
            lBin.Items.Add("Starting");
            aC.pec0();  // executes and formats cycle activity listBox
            foreach (string s in aC.lB1)    // maybe pass lBin to aC ??? TBD
                lBin.Items.Add(s);
        }

        public class AdrCtl
        {
            public List<string> lB1 = new List<string>();   // need constructor to ref callers objects
            public List<Parser.Cvbl> cV;
            public StkRam cStk = new StkRam(new uint[64]);
            public VRam vbls;
            public CtlRom sCwA;
            public alu Alu = new alu();

            //string[] ixf1, ixf2;
            //int ix1, ix2;
            //char[] bkts = new char[] { '[', ']' };
            //uint[] nMsk = new uint[33]  {0x0000, 0x0001, 0x0003, 0x0007, 0x000f,
            //                                   0x001f, 0x003f, 0x007f, 0x00ff,
            //                                   0x01ff, 0x03ff, 0x07ff, 0x0fff,
            //                                   0x1fff, 0x3fff, 0x7fff, 0xffff,
            //                                   0x0001ffff, 0x0003ffff, 0x0007ffff, 0x000fffff,
            //                                   0x001fffff, 0x003fffff, 0x007fffff, 0x00ffffff,
            //                                   0x01ffffff, 0x03ffffff, 0x07ffffff, 0x0fffffff,
            //                                   0x1fffffff, 0x3fffffff, 0x7fffffff, 0xffffffff};

            //uint mk_fld(string fld, out string[] ix)
            //{
            //    ix = fld.Trim().Split(bkts, StringSplitOptions.RemoveEmptyEntries);
            //    xParse(ix[1], out ix1);
            //    xParse(ix[2], out ix2);
            //    uint nM1;
            //    if (ix1 > ix2)
            //    {
            //        nM1 = nMsk[ix1 - ix2 + 1];
            //        return (nM1 <<= ix2);
            //    }
            //    else
            //    {
            //        nM1 = nMsk[ix2 - ix1 + 1];
            //        return (nM1 <<= ix1);
            //        }
            //    }

            private bool xParse(string src, out int xval)
            {
                if (src.Length > 2 && (src[1] == 'x' || src[1] == 'X'))
                {
                    if (!Int32.TryParse(src.Substring(2), NumberStyles.HexNumber, null, out xval))
                    {
                        MessageBox.Show("num Parse error");
                        return false;
                    }
                }
                else
                {
                    if (!Int32.TryParse(src, out xval))
                    {
                        MessageBox.Show("num Parse error");
                        return false;
                    }
                }
                return true;
            }

            //_|c0|_______________|c0|________________|c0|_______________|c0|_______________|c0|___
            //                    sCwA[2]                                eop ? sCwA[cwCt.qb] : 0
            //                    sCwB[0]              sCwB[cStk.qb + 1] eop ? 0 : sCwB[cStk.qb + 1] 
            //                                         va[sCwA.qa]       pway ? pway@ 
            //                                                            : etime ? cStk.qa
            //                                         vb[pop ? cStk.qa
            //                                          : etime ? sCwA.qb 
            //                                           : sCwA.qb]                            
            //                    cwCt.qa = 2		   cwCtA = cwCt.qa + 1 

            void nxtV(bool etime, bool eop, bool pway, bool tna, bool fna, bool gtr, bool eql, bool less, bool push, bool pop, bool call, bool rtn)
                {
                }

            public void pec0()
            {
                // initialize call stack
                cStk.adra = 0;
                cStk.wrena = true;
                cStk.adrb = 1;
                cStk.wrenb = true;
                cStk.dina = ((uint)vbls.vbls.Capacity - 1);
                cStk.dinb = 0;
                cStk.clka();    // clock stack memory to initialize -- 
                cStk.clkb();

                // start execution 
                int cycle = 0;
                uint callPtr = 0;
                string fmt;
                while (lB1.Count < 200) // limit execution to 200 logs
                {
                    if (rtn && !call)   //  end of uCwds
                        return;
                    // nxtV does nothing, call makes the parm values visible
                    // nxtV(etime, endop, pway, tjmp, fjmp, gtr, eql, less, push, pop, call, rtn);
                    // callCt, base;  stkCt, cwCt;  callCt is the call stack ptr

                    // address calculation for operands
                    vadra = pway ? (sCwA.qb & 0xffff) + (cStk.qb >> 16)
                            : !etime && !call ? (sCwA.qa >> 16) + (cStk.qb >> 16)
                            : etime && call ? cStk.qa
                            : pop ? cStk.qa + 1
                            : etime ? cStk.qa : 0;
                    vadrb = pop ? cStk.qa
                        : (etime || call) && !endop ? sCwA.qb + (cStk.qb >> 16)
                        : (!etime && !call) || endop ? (sCwA.qa & 0xffff) + (cStk.qb >> 16) : 0;

                    // address calculation for cwds
                    sadra = (UInt16)(!etime && !endop ? (cStk.qb & 0xffff) == 0 ? 2 : cStk.qb + 1
                        : etime ? (cmet || call && endop && !rtn) ? sCwA.qb
                        : (endop || push) ? call ? 0 : (cStk.qb + 1) : 0 : cStk.qb + 1);
                    sadrb = (UInt16)(etime && endop || push && !call || !etime && pway ? 0 : cStk.qb + 1);

                    // visualize cycle activity
                    fmt = String.Format("cycle = {0} {1}{2}{3}{4}{5}{6} Op1 = {7}; {8} Op2 = {9}; Alu = {10} {11} {12}"
                     , cycle, (Parser.ucdEnum)(sCwA.qb & 0xfff0), (tjmp ? ",tna " : ""), (fjmp ? ",fna " : "") // 0, 1, 2, 3
                     , (gtr ? ",gtr " : ""), (eql ? ",eql " : ""), (less ? ",less" : "") // 4, 5, 6
                     , vbls.qa, (Parser.ucdEnum)((sCwA.qb >> 16) & 0x01f), vbls.qb, Alu.qa// 7, 8, 9
                     , wrtVa ? (("; wrt " + Alu.qa + " to ") + ((pway && !call) ? (int)vadra < cV.Count ? cV[(int)vadra].name : "vbls[" + vadra + "]" : "TOS")) : ""
                     //                                                                  ----------------------------------------------------
                     //                                          --------------------------------------------------------------------------- 
                     // ------------------------------------------------------------------------------------------------------------------------
                     , cmet ? " cmet " : ""); //
                        lB1.Add(fmt);
                        if (lB1.Count >= 100)
                            return;
                        //c0  
                        sCwA.adra = (UInt16)sadra;   // (((UInt16)cStk.qb) == 0 ? 2 : cStk.qb + 1);
                        sCwA.adrb = (UInt16)sadrb;  // (endop ? 0 : cStk.qb + 1);
                        AdrCtl myac = new AdrCtl();
                        //vbls.ac = myac;
                        //myac.vbls = vbls;
                        //int x = vbls.ac.vbls.vbls.Count;
                        // callPtr, base;  stkPtr, cwPtr;
                        // callPtr, stkPtr; base, cwPtr;
                        // caller pushes args, uses caller base to get args, pushes parms to stk
                        // call || push for first arg push call stk stk ct and base;  then push at endop new base and cwCt
                        // at endop stkCt is new base, fn.cwix is new cwCt

                        // look at controls
                        nxtV(etime, endop, pway, tjmp, fjmp, gtr, eql, less, push, pop, call, rtn);

                        //   cStk.qa [callPtr][base]   cStk.qb [stkCt][cwCt]
                        callPtr = call && !rtn && endop ? (cStk.qa >> 16)
                            : call && rtn ? (cStk.qa >> 16)
                            : cStk.qa >> 16;
                        cStk.adra = call && push ? callPtr + 2 : callPtr;
                        cStk.adrb = call ? (push || endop) ? callPtr + 1 : callPtr - 1 : callPtr + 1; // always calllPtr + 1 ????????
                        cStk.wrena = call && rtn ? false : true;
                        cStk.wrenb = call && rtn ? false : true;
                        cStk.dina = pop ? cStk.qa + 1
                            : call ? push ? (uint)(((callPtr & 0xffff) + 2) << 16) | (UInt16)(cStk.qa - 1)
                            : rtn ? (uint)(((callPtr & 0xffff) - 2) << 16) | (UInt16)(cStk.qa - 1)
                            : (uint)((callPtr & 0xffff) << 16) | (UInt16)(cStk.qa - 1)
                            : push ? cStk.qa - 1 : cStk.qa;
                        cStk.dinb = call && endop && !rtn ? cStk.qa << 16 | (UInt16)sCwA.qb : cStk.qb & 0xffff0000
                            | (UInt16)((cmet || call && endop && !rtn) ? sCwA.qb
                            : cStk.qb + 1);
                        //: endop ? 0 : cStk.qb + 1;
                        //(endop || push) ? call ? 0 : (cStk.qb + 1) : 0);
                        //((push || (call && !rtn) ? (cStk.qa - 0x00010000) : pop ? (cStk.qa + 0x00010000) : cStk.qa
                        //| (call && endop && !rtn ? (cStk.qa & 0xffff0000) | (UInt16)sCwA.qb
                        //: (cStk.qb >> 16) << 16) | (UInt16)(cStk.qb == 0 ? 2 : cmet ? sCwA.qb : cStk.qb + 1)));
                        //callPtr = call ? push ? (cStk.qa >> 16) + 2 : rtn ? (cStk.qa >> 16) - 2 : (cStk.qa >> 16) : (cStk.qa >> 16);
                        //cStk.adra = callPtr;
                        //cStk.adrb = call && !endop ? callPtr - 1 : callPtr + 1;
                        ////| (call && endop && rtn ? callPtr + 1 : 0);
                        //cStk.wrena = call && rtn ? false : true;
                        //cStk.wrenb = call && rtn ? false : true;
                        //cStk.dina = callPtr << 16 | (UInt16)(push || (call && !rtn) ? cStk.qa - 1 : pop ? cStk.qa + 1 : cStk.qa);
                        //cStk.dinb = call && endop && !rtn ? ((cStk.qa) << 16) | (UInt16)(sCwA.qb) : ((cStk.qb >> 16) << 16)
                        //    | (UInt16)((cStk.qb == 0 ? 2 : cmet ? sCwA.qb : cStk.qb + 1));

                    // new values for variables memory
                        vbls.dina = Alu.qa;
                        vbls.adra = (UInt16)vadra;
                        vbls.adrb = (UInt16)vadrb;
                        vbls.wrena = wrtVa ? true : false;
                        if (wrtVa)
                        { }
                   // two step "clocking" emulates hardware parallelism
                        sCwA.clka();
                        sCwA.clkb();
                        vbls.clka();
                        vbls.clkb();
                        if (sCwA.qa == 0xffffffff || sCwA.qb == 0xffffffff)
                        // end of of uCwds 
                            return;
                    // two step clock cStk to next, target, call, or return
                        cStk.clka();
                        cStk.clkb();
                        cycle++;
                        uint dcd = (sCwA.qb >> 16);
                        //   sCwA.adra = 0; sCwA.adrb = 0; vbls.adra = 0; vbls.adrb = 0;
                        String cbits = ((Parser.ucdEnum)(dcd & 0XFFF0)).ToString(), aluCode;  //, sCwd, oCwd;

                        //____________________|c0|________________|c0|_______________|c0|_______________|c0|___
                        //                    sCwA[2]                                eop ? sCwA[cwCt.qa] : 0
                        //                    sCwB[0]             sCwB[cStk.qb + 1]  eop ? 0 
                        //                                                            : sCwB[cStk.qb + 1] 
                        //                                        va[sCwA.qa]        pway ? pway@ 
                        //                                                            : etime ? cStk.qa
                        //                                        vb[pop ? cStk.qa
                        //                                         : etime ? sCwA.qb 
                        //                                         : sCwA.qb]                            
                        //                    cwCt.qa = 2		  cwCtA = cwCt.qa + 1 

                        //string a = "adra", b = "adrb", typ, nm;
                        //int flgs = (int)(Parser.ops.etime |Parser.ops.pway | Parser.ops.eop);

                        aluCode = ((Parser.ucdEnum)((sCwA.qb >> 16) & 0X1F)).ToString();

                        //    string curcbits = sCwA.cwAlways[sCwA.cwAlways.Count - 1];
                        //sadra = sadrA; sadrb = sadrB; vadrb = (uint)vadrB;
                        //if (vbls.adra != vadrA || sCwA.adra != sadrA)
                        //{ }
                    }
                }
                //call = 0x8000,  // 
                //rtn = 0x4000,  // 
                //tna = 0x2000,  // 0x000E combines with cond opers and uses spare opcodes
                //fna = 0x1000,  // 0x000F
                //eop = 0x0800,  //
                //gtr = 0x0410,
                //eql = 0x0210,  // !eql = 0x0510
                //less = 0x0110,
                //pway = 0x0080,  // 
                //push = 0x0050,  // may use 0x8040 to stack call parms, or with ALU codes 
                //pop = 0x0030,  // 
                //etime = 0x0010,

                //  10/5/13
                //_|c0|_______________|c0|________________|c0|_______________|c0|_______________|c0|___
                //          	      sCwA[2]                                eop ? sCwA[cwCt.qa + 1] : 0
                //					  sCwB[0]             sCwB[cwCt.qa + 1]  eop ? 0 : sCwB[cwCt.qa + 1] 
                //										  va[sCwA.qa]        pway ? pway@ : etime ? stkCt.qa]
                //										  vb[pop ? stkCt.qa : etime ? sCwB.qb : sCwA.qb]                            
                //                    cwCt.qa = 2         cwCtA = cwCt.qa + 1 
                ///* push base | stkct to use as part of call to pass args  call push pway
                // * push args   push pway
                // * push rtnct | cwct 
                // * stkct to base, cwct =fun@   call endop cwct[ocwB]
                // * dofnctn
                // * restore counts wrt return to TOS call pop   

                //public bool lesscnd
                //{ get { return less & Alu.alb > 0 ? true : false; } }
                //public bool gtrcnd
                //{ get { return gtr & Alu.agb > 0 ? true : false; } }
                //public bool eqlcnd
                //{ get { return eql & Alu.aeb > 0 ? true : false; } }

                /*
                public int sadrA
                { get { return endop || push ? cmet ? (int)(sCwA.qb & 0x0000FFFF) : cStk.qb + 1 : etime ? 0 : cStk.qb == 0 ? 2 : cStk.qb; } }
                public int sadrB
                { get { return etime ? endop ? 0 : cStk.qb + 1 : cStk.qb == 0 ? 2 : cStk.qb + 1; } }
                public int nxtCw
                { get { return cStk.qb == 0 ? 2 : cmet ? (int)(sCwA.qb & 0x0000FFFF) : cStk.qb + 1; } }
                public int vadaa
                { get { return (pop ? (int)cStk.qa + 1 : pwaybit ? sCwA.qb & 0xffff : etime ? (int)cStk.qa : sCwA.qa >> 16); } }
                public int vada
                { get { return pop ? call ? cStk.qa + 2 : (int)cStk.qa + 1 : pwaybit ? sCwA.qb & 0xffff : etime ? (int)cStk.qa : (sCwA.qa >> 16) + ((cStk.qa & 0xffff000) >> 16); } }
                public int vadbb
                { get { return pop ? (int)cStk.qa : etime ? endop ? 0 : (int)sCwA.qb & 0xFFFF : (int)sCwA.qa & 0xFFFF; } }
                public int vadb
                { get { return pop ? call ? cStk.qa + 1 : (int)cStk.qa : etime ? endop ? 0 : (int)sCwA.qb & 0xFFFF : (int)(sCwA.qa & 0xFFFF) + ((cStk.qa & 0xffff000) >> 16); } }

                public int wrtVa
                { get { return etime ? (pop || tjmp || fjmp) ? 0 : 1 : pwaybit ? 1 : 0; } }
                */
                // */

                private uint sadra, sadrb, vadra, vadrb;

                //public uint sadrA
                //{ get { return endop && cmet ? (UInt16)sCwA.qb : push || endop ? cStk.qb + 1 : etime ? 0 : (cStk.qb == 0 ? 2 : cStk.qb); } }
                //   { get { return (endop || push) ? (cmet ? (int)(sCwA.qb & 0x0000FFFF) : cStk.qb + 1) : ((etime || call) ? 0 : (cStk.qb == 0 ? 2 : cStk.qb)); } }

                //public uint sadrB
                //{ get { return etime ? endop ? call ? cStk.qb + 2 : 0 : cStk.qb + 1 : cStk.qb == 0 ? 0 : push ? cStk.qb + 2 : cStk.qb + 1; } }
                public uint nxtCw
                { get { return cStk.qb == 0 ? 2 : cmet ? (UInt16)sCwA.qb : cStk.qb + 1; } }
                //public int vadaa
                //{ get { return (pop ? (int)cStk.qa + 1 : pwaybit ? sCwA.qb & 0xffff : etime ? (int)cStk.qa : sCwA.qa >> 16); } }
                public int vadrA
                { get { return (int)(pop ? cStk.qa + 1 : pway ? call ? cStk.qa : sCwA.qb & 0xffff : etime ? cStk.qa : sCwA.qa >> 16); } }
                //public int vadbb
                //{ get { return pop ? (int)cStk.qa : etime ? endop ? 0 : (int)sCwA.qb & 0xFFFF : (int)sCwA.qa & 0xFFFF; } }
                public int vadrB
                { get { return (int)(pop ? cStk.qa : (etime || call) && !endop ? (sCwA.qb & 0xFFFF) : (sCwA.qa & 0xFFFF)); } }

                public bool wrtVa
                { get { return etime && !(pop || tjmp || fjmp) || pway; } }

                public bool etime
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.etime) == (uint)Parser.ucdEnum.etime ? true : false; } }
                public bool push
                { get { return ((sCwA.qb >> 16) & (uint)Parser.ucdEnum.push) == (uint)Parser.ucdEnum.push ? true : false; } }
                public bool pop
                { get { return ((sCwA.qb >> 16) & (uint)Parser.ucdEnum.pop) == (uint)Parser.ucdEnum.pop ? true : false; } }
                public bool pway
                { get { return ((sCwA.qb >> 16) & (uint)Parser.ucdEnum.pway) == (uint)Parser.ucdEnum.pway ? true : false; } }
                public int fSel
                { get { return (int)((sCwA.qb >> 16) & 0xfff); } }
                public bool endop
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.eop) == (int)Parser.ucdEnum.eop ? true : false; } }
                public bool call
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.call) == (int)Parser.ucdEnum.call ? true : false; } }
                public bool rtn
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.rtn) == (int)Parser.ucdEnum.rtn ? true : false; } }
                public bool less
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.less) == (int)Parser.ucdEnum.less ? true : false; } }
                public bool gtr
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.gtr) == (int)Parser.ucdEnum.gtr ? true : false; } }
                public bool eql
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.eql) == (int)Parser.ucdEnum.eql ? true : false; } }
                public bool tjmp
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.tna) == (int)Parser.ucdEnum.tna ? true : false; } }
                public bool fjmp
                { get { return ((sCwA.qb >> 16) & (int)Parser.ucdEnum.fna) == (int)Parser.ucdEnum.fna ? true : false; } }
                public bool lesscnd
                { get { return less & Alu.alb > 0 ? true : false; } }
                public bool gtrcnd
                { get { return gtr & Alu.agb > 0 ? true : false; } }
                public bool eqlcnd
                { get { return eql & Alu.aeb > 0 ? true : false; } }
                public bool cmet
                {
                    get
                    {
                        return tjmp & !fjmp & (lesscnd | gtrcnd | eqlcnd) ? true : // jmp ? true : 
                            (fjmp & !tjmp & (!lesscnd & !gtrcnd & !eqlcnd)) ? true : tjmp & fjmp ? true : false;
                    }
                }
            }
            //  10/5/13
            //_|c0|_______________|c0|________________|c0|_______________|c0|_______________|c0|___
            //          	      sCwA[2]                                eop ? sCwA[cwCt.qa + 1] : 0
            //					  sCwB[0]             sCwB[cwCt.qa + 1]  eop ? 0 : sCwB[cwCt.qa + 1] 
            //										  va[sCwA.qa]        pway ? pway@ : etime ? stkCt.qa]
            //										  vb[pop ? stkCt.qa : etime ? sCwB.qb : sCwA.qb]                            
            //                    cwCt.qa = 2         cwCtA = cwCt.qa + 1 
            // push cwCt and args, pop first two, wrt rslt & read nxt end with rslt in TOS.  return wrts TOS to pway loc, load cwCt
            // TOS can wrt toswitch (cbits)
            //{
            //    case "nop":
            //        sCwA.adra = cStk.qb;
            //        sCwA.cwAlways.Add(cbits + " : adra <= cStk.qb" );
            //        if (sCwA.adra != sadrA)
            //        { }
            //        sCwA.adrb = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adrb <= (cStk.qb + 1)");
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        vbls.adra = sCwA.qa >> 16 & 0xffff;
            //        vbls.cwAlways.Add(cbits + " : adra <= (sCwA.qa >> 16 + (cStk.qa & 0xffff000) >> 16)");
            //        if (vbls.adra != vadrA)
            //        { }
            //        vbls.adrb = (sCwA.qa & 0xFFFF) + ((cStk.qa & 0xffff000) >> 16);
            //        vbls.cwAlways.Add(cbits + " : adrb <=((sCwA.qa & 0xFFFF) + ((cStk.qa & 0xffff000) >> 16))");
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "etime, pway, eop":
            //        sCwA.adra = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adra <= cStk.qb");
            //        if (sCwA.adra != sadrA)
            //        { }
            //        sCwA.adrb = 0;
            //        sCwA.cwAlways.Add(cbits + " : adrb <= 0");
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        vbls.adra = sCwA.qb & 0xffff;
            //        vbls.cwAlways.Add(cbits + " : adra <= (sCwA.qb & 0xffff)");
            //        if (vadrA != (sCwA.qb & 0xffff) || sadrB != 0 || wrtVa != 1 || sadrA != cStk.qb + 1)
            //        { }
            //        vbls.adrb = 0;
            //        vbls.cwAlways.Add(cbits + " : adrb <= 0");
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "less, eop, tna":
            //    case "less, eop, fna":
            //    case "eql, eop, tna":
            //    case "eql, eop, fna":
            //    case "gtr, eop, tna":
            //    case "gtr, eop, fna":
            //        vbls.adra = cStk.qa;
            //        sCwA.adra = cmet ? (int)sCwA.qb & 0x0000FFFF : cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adra <= ( cmet ? sCwA.qb & 0x0000FFFF : cStk.qb + 1)");
            //        if (sCwA.adra != sadrA)
            //        { }
            //        //sCwA.adrb = cStk.qb + 1;
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        if (sCwA.adra != sadrA)
            //        { }
            //        vbls.adrb = 0;
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "pway, eop":
            //        oCwd = cbits + " " + aluCode;
            //        sCwA.adra = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adra <= (cStk.qb + 1).ToString()");
            //        if (sCwA.adra != sadrA)
            //        { }
            //        sCwA.adrb = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adrb <= (cStk.qb + 1).ToString()");
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        vbls.adra = sCwA.qb & 0xffff;
            //        vbls.cwAlways.Add(cbits + " : adra <= (sCwA.qb & 0xffff)");
            //        if (vbls.adra != vadrA)
            //        { }
            //        vbls.adrb = (int)(sCwA.qa & 0xFFFF) + ((cStk.qa & 0xffff000) >> 16);
            //        vbls.cwAlways.Add(cbits + " : adrb <= " + (sCwA.qa & 0xFFFF) + ((cStk.qa & 0xffff000) >> 16));
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "push":
            //        sCwA.adra = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adra <= (cStk.qb + 1)");
            //        if (sCwA.adra != sadrA)
            //        { }
            //        sCwA.adrb = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adrb <= (cStk.qb + 1)");
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        oCwd = cbits + " " + aluCode;
            //        vbls.adra = cStk.qa;
            //        if (vbls.adra != vadrA)
            //        { }
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "push, pway":
            //        vbls.adra = sCwA.qb & 0xffff;
            //        if (vbls.adra != vadrA)
            //        { }
            //        if (vbls.adrb != vadrB)
            //        { }
            //        if (sCwA.adra != sadrA)
            //        { }
            //        //sCwA.adrb = cStk.qb + 1;
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        break;
            //    case "pop":
            //        sCwA.adrb = cStk.qb + 1;
            //        sCwA.cwAlways.Add(cbits + " : adrb <= (cStk.qb + 1)");
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        if (sCwA.adra != sadrA)
            //        { }
            //        vbls.adra = cStk.qa + 1;
            //        if (vbls.adra != vadrA)
            //        { }
            //        vbls.adrb = (int)cStk.qa;
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "etime":
            //        //sCwA.adrb = cStk.qb + 1;
            //        if (sCwA.adra != sadrA)
            //        { }
            //        sCwA.adrb = cStk.qb + 1;
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        vbls.adra = cStk.qa;
            //        if (vbls.adra != vadrA)
            //        { }
            //        vbls.adrb = (int)sCwA.qb & 0xFFFF;
            //        if (vbls.adrb != vadrB)
            //        { }
            //        break;
            //    case "etime, push":
            //        vbls.adra = cStk.qa;
            //        oCwd = cbits + " " + aluCode;
            //        if (vadrA != cStk.qa)
            //        { }
            //        // sCwA.adrb = cStk.qb + 1;
            //        if (sCwA.adrb != sadrB)
            //        { }
            //        break;
            //    case "eop, less":

            //        break;
            //    case "eop, eql":

            //        break;
            //    case "eop, gtr":

            //        break;
            //    case "rtn":
            //        return;
            //        break;
            //    default:
            //        break;
            //}

            public class alu
            {
                //    cvblsa, cvblsb, fSel, qa, alb, aeb, agb, stka, stkb, avbl, bstk
                public CEngine.AdrCtl ac;
                public int alb { get { return (ac.vbls.qa < ac.vbls.qb) ? 1 : 0; } }
                public int aeb { get { return (ac.vbls.qa == ac.vbls.qb) ? 1 : 0; } }
                public int agb { get { return (ac.vbls.qa > ac.vbls.qb) ? 1 : 0; } }
                public int qa
                {
                    get
                    {
                        switch ((Parser.ucdEnum)(ac.fSel & 0x1f))
                        {
                            case Parser.ucdEnum.add: return (int)(ac.vbls.qa + ac.vbls.qb);
                            case Parser.ucdEnum.sub: return (int)(ac.vbls.qa - ac.vbls.qb);
                            case Parser.ucdEnum.mpy: return (int)(ac.vbls.qa * ac.vbls.qb);
                            case Parser.ucdEnum.dvd: return (int)(ac.vbls.qa / ac.vbls.qb);
                            case Parser.ucdEnum.mlo: return (int)(ac.vbls.qa % ac.vbls.qb);
                            case Parser.ucdEnum.bnd: return (int)(ac.vbls.qa & ac.vbls.qb);
                            case Parser.ucdEnum.bor: return (int)(ac.vbls.qa | ac.vbls.qb);
                            case Parser.ucdEnum.nop: return (int)(ac.vbls.qa);
                            case Parser.ucdEnum.etime: return (int)(ac.vbls.qb);
                            default: return -1;
                        }
                    }
                }
            }
            //public class dff
            //{
            //    public dff(uint val)
            //    { myqa = val; }
            //    uint mydin = 0, myqa = 0;
            //    public uint din
            //    { set { mydin = value; } }
            //    public uint width
            //    { set { } }
            //    public uint qa
            //    { get { return myqa; } }
            //    public void clk()
            //    { myqa = mydin; }



            public class StkRam
            {
                public StkRam(uint[] ram)
                {
                    sram = ram;
                }
                uint[] sram;
                uint inadra, inadrb, indina, indinb, adrA = 0, adrB = 0;
                bool cena = true, cenb = true;
                public bool wrena = true, wrenb = true;
                public bool clkena
                { set { cena = value; } }
                public bool clkenb
                { set { cenb = value; } }
                public uint dina
                { set { indina = value; } }
                public uint dinb
                { set { indinb = value; } }
                public uint adra
                { set { inadra = value; } }
                public uint adrb
                { set { inadrb = value; } }
                public uint qa
                { get { return sram[(int)adrA]; } }
                public uint qb
                { get { return sram[(int)adrB]; } }
                public void clka()
                { if (cena) adrA = inadra; if (wrena) sram[inadra] = indina; }
                public void clkb()
                { if (cenb) adrB = inadrb; if (wrenb) sram[inadrb] = indinb; }
            }


            public class CtlRom
            {
                public CtlRom(List<uint> rom)
                {
                    srom = new List<uint>(rom);
                }
                public List<uint> srom = new List<uint>();
                uint inadra, inadrb, adrA = 0, adrB = 0;
                bool cena = true, cenb = true;
                public bool clkena
                { set { cena = value; } }
                public bool clkenb
                { set { cenb = value; } }
                public uint adra
                { set { inadra = value; } }
                public uint adrb
                { set { inadrb = value; } }
                public uint qa
                { get { return srom[(int)adrA]; } }
                public uint qb
                { get { return srom[(int)adrB]; } }
                public void clka()
                { if (cena) adrA = inadra; }
                public void clkb()
                { if (cenb) adrB = inadrb; }
            }

            public class VRam
            {
                public VRam(int[] vsz)
                {
                    vbls = new List<int>(256);
                }
                public VRam()
                {
                    vbls = new List<int>();
                }
                public List<int> vbls;
                public List<String> cwAlways = new List<string>();

                uint inadra, inadrb, adrA = 0, adrB = 0;
                int mydina, mydinb;
                bool mywrena, mywrenb;
                CEngine.AdrCtl myac;
                public CEngine.AdrCtl ac
                { get { return myac; } set { myac = value; } }
                public uint adra
                { get { return inadra; } set { inadra = value; } }
                public uint adrb
                { get { return inadrb; } set { inadrb = value; } }
                public int dina
                { set { mydina = value; } }
                public int dinb
                { set { mydinb = value; } }
                public bool wrena
                { set { mywrena = value; } }
                public bool wrenb
                { set { mywrenb = value; } }
                public int qa
                { get { return adrA < vbls.Count ? vbls[(int)adrA] : 0; } }
                public int qb
                { get { return adrB < vbls.Count ? vbls[(int)adrB] : 0; } }
                public void clka()
                { adrA = inadra; if (mywrena) vbls[(int)adrA] = mydina; }
                public void clkb()
                {
                    adrB = inadrb;
                    if (mywrenb) vbls[(int)adrB] = mydinb;
                }
            }


            char[] copers1 = new char[] { '+', '-', '!', '~', '=', '?', ':', '&', '|', '^', '*', '/', '<', '>', '%' };
            char[] wsp = new char[] { };


            public int datx(string dexp)
            {
                string[] bnms;
                Stack<int> dstk = new Stack<int>();
                int opix = 0;
                opix = dexp.IndexOfAny(copers1);
                bnms = dexp.Split(wsp, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in bnms)
                {
                    if (Char.IsLetterOrDigit(s[0]))
                    {
                        if (s[0] == '~')
                        {
                            dstk.Push(~dstk.Pop());
                        }
                        else
                        {
                            // dstk.Push(do_op(s, dstk));
                        }
                    }
                }
                return dstk.Pop();
            }
        }
    }
