using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace CEngine16
{


    public class Parser
    {
        //List<int> cas = new List<int>();
        //List<int> ccs = new List<int>();
        public List<uint> uCwds = new List<uint>();
        List<uint> Xcws = new List<uint>();
        public Cvbl newV = new Cvbl();
        CStmnt cS; // = new CStmnt();
        public List<Cvbl> cvbls = new List<Cvbl>();
        public char[] xOpers = new char[] { '+', '-', '!', '~', '=', '?', ':', '&', '|', '^', '*', '/', '<', '>', '%' };  // 71, 94 , 364
        char[] cBs = new char[] { '{', '}' };
        char[] wsp = new char[] { };
        char[] expOps = new char[] { ';', '=', '+', '-', '!', '~', '(', ')', '?', ':', '&', '|', '^', '*', '/', '<', '>', '%' };
        string[] inSplitA;
        string token = " ";
        int lineno, cbcnt = 0;
        string[] inlineSplit;
        FileStream inStr;
        StreamReader sRdr;
        StringBuilder sBin;
        List<String> OutList = new List<string>();
        public ListBox lB1;
        public ListBox lB2;
        public ListBox lB3;
        public CStmnt cStmnt;
        public CFunc fmain;
        public CFunc cfn;
        public class rop { public string oper; public int prec; public int opcw; }
        public Stack<rop> ostk = new Stack<rop>();
        public string seq = "";
        public bool reset;
        public StringBuilder rpb = new StringBuilder();
        List<uint> cList = new List<uint>();
        LinkedList<CStmnt> sXList = new LinkedList<CStmnt>();
        LinkedListNode<CStmnt> llN;
        List<String> xS = new List<string>();
        public List<string> sxl = new List<string>();
        char[] delims = new char[] { '=', '(', ')', '{', '}', ';' };
        char[] rparenDelims = new char[] { '=', ')', '{', '}', ';' };
        char[] semic = new char[] { ';' };
        char[] rparen = new char[] { ')' };
        string[] sdelims = new string[] { "(", ")", "{", "}", "=", " " };
        List<String> cwDcd = new List<string>();
        int delimIx = 0;
        public struct vals { public int time; public int val; };
        public int scn_tm;

        // begin parses source and calls cKwds to generate micro program.
        public void begin(String fPath, ListBox lB1in, CEngine pE)
        {
            lB1 = lB1in;
            inStr = new FileStream(fPath, FileMode.Open);
            sRdr = new StreamReader(inStr);
            CStmnt smain;
            //bld.bC();
            sBin = new StringBuilder();
            sBin.EnsureCapacity(128);
            uCwds.Add(0);
            uCwds.Add(0);
            lineno = 0;
            delimIx = nxtDelim();  // returns index of next delim in sBin
            if (sBin.ToString().Substring(0, delimIx).Trim() == "main")
            {
                if (delimIx >= 0 && sBin[delimIx] == '(')
                {
                    delimIx = nxtDelim(rparenDelims);
                    if (delimIx >= 0 && sBin[delimIx] == ')')
                    {
                        sBin.Remove(0, delimIx + 1);
                        smain = new CStmnt();
                        smain.name = "main";
                        smain.kwd = "main";
                    }
                }
                else
                {
                    MessageBox.Show("main syntax err");
                    return;
                }
                Cvbl cv = new Cvbl();
                cv.name = "0";
                cv.vix = add_Cvbl(cv);
                cv = new Cvbl();
                cv.name = "1";
                cv.vix = add_Cvbl(cv);
                cv = new Cvbl();
                cv.name = "NOS";
                cv.vix = add_Cvbl(cv);
                fmain = new CFunc();
                fmain.name = "main";
                delimIx = nxtDelim();
                nxtStmnt(ref token, ref inlineSplit, delims, uCwds, cvbls);
                // returns at EOF
            }
            else
            {
                MessageBox.Show("Missing main function");
                return;
            }
            fmain.vcnt = cvbls.Count;
            uCwds.Add(0);
            uCwds.Add((int)ucdEnum.rtn << 16);
            uCwds.Add(0);
            uCwds.Add((int)ucdEnum.rtn << 16);
            uint callix = (uint)uCwds.Count + 1;
            foreach (CFunc f in cfuns)
            {
                foreach (int x in f.calls)
                    uCwds[x] |= (uint)uCwds.Count;
                sBin.Insert(0, f.sb);
                delimIx = nxtDelim();
                nxtStmnt(ref token, ref inlineSplit, delims, uCwds, f.cvbls);
                if (((ucdEnum)(uCwds[uCwds.Count - 1] >> 16) & ucdEnum.eop) == ucdEnum.eop)
                    uCwds[uCwds.Count - 1] ^= (int)ucdEnum.eop << 16;
                uCwds[uCwds.Count - 1] |= ((uint)(ucdEnum.call | ucdEnum.rtn) << 16);
                uCwds.Add(0);
                uCwds.Add(0);
            }
            inStr.Close();
            MessageBox.Show("End of file");
            //pE.ipl(lB1, uCwds, cvbls);
            //FileStream log = new FileStream("logOut.txt", FileMode.OpenOrCreate);
            //StreamWriter lwrt = new StreamWriter(log);
            //foreach (string s in lB1.Items)
            //{
            //    lwrt.WriteLine(s);
            //}
            //lwrt.Flush();
            //log.Close();
            return;
        }


        public CStmnt cKwds(LinkedList<CStmnt> sXList, List<Cvbl> vbls, List<uint> Scws)
        {
            CStmnt cS = new CStmnt();
            int lp = 0, rp = 0, tIx = 0;
            char[] semi = new char[] { ';' };
            char[] keychars = new char[] { 'i', 'e', 'f', 'w', 'd', 's' };
            string[] keywords = new string[] { "=", "(", "{", "}", "if", "else", "for", "while", "do", "switch" };

            switch (token.Trim())
            {
                case "if":
                    CIf cIf = (CIf)sXList.Last.Value;
                    cIf.cond = sBin.ToString().Substring(0, sBin.ToString().IndexOf(')') + 1);
                    rplSB(cIf.cond);
                    sBin.Remove(0, cIf.cond.Length);
                    cIf.cond = rpb.ToString();
                    makCond(cIf.cond, vbls, Scws);
                    Scws[Scws.Count - 1] |= (uint)((int)(ucdEnum.eop | ucdEnum.fna) << 16 | cIf.fcx);
                    cIf.fcx = Scws.Count;  // fcx saves cc Ix for false target
                    Scws.Add(0); // update after true statement(s)
                    nxtStmnt(ref token, ref inlineSplit, delims, Scws, vbls);  // ckwds(sXList);  // cIf.tList);
                    Scws[cIf.fcx] = (uint)Scws.Count; // 
                    if (sBin.ToString().IndexOf("else") >= 0)
                    {
                        sBin.Remove(0, sBin.ToString().IndexOf("else") + 4);
                        //cas.Add(ccs.Count);
                        //cas.Add(0);
                        //ccs.Add((int)(ucdEnum.tna | ucdEnum.fna)); // insert jump over else 
                        //cIf.enx = ccs.Count; // ccs 
                        //ccs.Add(0);
                        //ccs[cIf.fcx] = cas.Count;
                        //cKwds(sXList, vbls, Scws);
                        //ccs[cIf.enx] = cas.Count; // come here to skip else true cond
                    }
                    return cIf;
                case "while":
                    CWhile cWhile = (CWhile)sXList.Last.Value;
                    rplSB(cWhile.cond);
                    //inSB.Remove(0, tokenIx + 1);
                    cWhile.cond = rpb.ToString();
                    //  10/5/13
                    //_|c0|_______________|c0|________________|c0|_______________|c0|_______________|c0|___
                    //          	      sCwA[2]                                eop ? sCwA[cwCt.qa] : 0
                    //					  sCwB[0]             sCwB[cwCt.qa + 1]  eop ? 0 : sCwB[cwCt.qa + 1] 
                    //										  va[sCwA.qa]        pway ? pway@ : etime ? stkCt.qa]
                    //										  vb[pop ? stkCt.qa : etime ? sCwB.qb : sCwA.qb]                            
                    //                    cwCt.qa = 2         cwCtA = cwCt.qa + 1 

                    makCond(cWhile.cond, vbls, Scws); // blds opwds for cond eval
                    cWhile.loopix = Scws.Count - 1;
                    Scws[Scws.Count - 1] |= (int)(ucdEnum.eop | ucdEnum.fna | ucdEnum.etime) << 16; // fcond jumps over body
                    cWhile.bodyix = Scws.Count;
                    delimIx = nxtDelim();
                    nxtStmnt(ref token, ref inlineSplit, delims, Scws, vbls);
                    //            cWhile.condix= mem.Count;
                    //makCond(cWhile.cond, vbls, Scws, Xcws);// blds opwds for cond eval
                    Scws.Add((uint)((int)(ucdEnum.eop | ucdEnum.tna | ucdEnum.etime) << 16 | cWhile.bodyix)); // fcond jumps over body
                                                                                                              //     mem.Add(0);
                    Scws[cWhile.loopix] |= (uint)Scws.Count; ;
                    //           mem[cWhile.loopix + 1] = (int)(ops.tna | ops.fna | ops.etime | ops.eop) << 16 | cWhile.condix;
                    return cWhile;

                case "for": // inits, fcond jumps over blk and post assigns
                    CFor cFor = (CFor)sXList.Last.Value;
                    inSplitA = cFor.xprn.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pfx in inSplitA) // 0 or more assignments
                    {
                        rplSB(pfx + ';');
                        mem_opwds(rpb.ToString(), Scws, vbls);
                    }
                    // for init done:  leave space in cas to go to cond eval then true cond repeats loop
                    rplSB(cFor.cond);
                    cFor.cond = rpb.ToString().Trim();
                    makCond(cFor.cond, vbls, Scws); // blds opwds for cond eval
                    Scws[Scws.Count - 1] |= (int)(ucdEnum.eop | ucdEnum.fna | ucdEnum.etime) << 16; // fcond jumps over body
                    cFor.bodyix = Scws.Count;
                    delimIx = nxtDelim();
                    nxtStmnt(ref token, ref inlineSplit, delims, Scws, vbls);
                    inSplitA = cFor.post.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pfx in inSplitA)
                    {
                        rplSB(pfx + ';');
                        mem_opwds(rpb.ToString(), Scws, vbls);
                        nxtDelim();
                    } // body and post-fix done
                    Scws[Scws.Count - 1] |= (uint)((int)(ucdEnum.eop | ucdEnum.tna | ucdEnum.etime) << 16 | cFor.bodyix); // fcond jumps over body
                    Scws[cFor.bodyix - 1] |= (uint)Scws.Count;
                    return cFor;
                case "do": // tcond at end to jump back
                    CWhile doWhile = (CWhile)sXList.Last.Value;
                    doWhile.loopix = Scws.Count;  // ix of "while" cwd used to insert tcx
                    sXList.AddLast(doWhile);
                    nxtStmnt(ref token, ref inlineSplit, delims, Scws, vbls);
                    nxtDelim();
                    inSplitA = sBin.ToString().Split(sdelims, StringSplitOptions.RemoveEmptyEntries);
                    if (inSplitA[0].TrimStart() == "while")
                        sBin.Remove(0, inSplitA[0].Length);
                    rpb.Remove(0, rpb.Length);
                    nxtDelim(rparen);
                    rplSB(sBin.ToString().Substring(0, sBin.ToString().IndexOf(')') + 1));
                    sBin.Remove(0, sBin.ToString().IndexOf(')') + 1);
                    doWhile.cond = rpb.ToString();
                    makCond(doWhile.cond, vbls, Scws);
                    Scws[Scws.Count - 1] |= (uint)((int)(ucdEnum.etime | ucdEnum.eop | ucdEnum.tna) << 16 | doWhile.loopix);
                    return doWhile;
                case "switch":
                    CStmnt cSwitch = new CStmnt();
                    cSwitch.xprn = inSplitA[0];
                    cSwitch.srcln = lineno;
                    sXList.AddLast(cSwitch);
                    //MessageBox.Show("Switch expr error");
                    //getNext();
                    break;
                /* function is a variable that accesses variables to compute the return value for the get accessor.
                 * Call pushes arguments on the stack that grows downward in vbl memory so the address is formed by 
                 * adding the index to stack pointer.  The main function pointer is zero so the variables index is
                 * used to address variables using the same mechanism.  The call bit addresses the function using
                 * the internal variable value and returns the ALU result value instead of the internal variable value.
                 */
                /* push base | stkct to use as part of call to pass args  call push pway
                 * push args   push pway
                 * push rtnct | cwct 
                 * stkct to base, cwct =fun@   call endop cwct[ocwB]
                 * dofnctn
                 * restore counts wrt return to TOS call pop
                 */
                case "fncall":
                    cS = new CStmnt();
                    cS.srcln = lineno;
                    cS.name = "funcall";
                    sXList.AddLast(cS);
                    if ((lp = sBin.ToString().IndexOf('(')) >= 0)
                        sBin.Remove(0, lp + 1);
                    else
                        MessageBox.Show("call parens error");
                    if ((rp = sBin.ToString().IndexOf(')')) >= 0)
                    {
                        cS.xprn = cvbls[get_vbl(sBin.ToString().Substring(0, lp).Trim(), vbls)].cfnref.sb.ToString();
                        sBin.Remove(0, rp + 1);
                    }
                    else
                        MessageBox.Show("call parens error");
                    if ((rp = sBin.ToString().IndexOf(')')) >= 0)
                        sBin.Remove(0, rp + 1);
                    break;
                case "vasgn":
                    cStmnt = new CStmnt();
                    rpb.Remove(0, rpb.Length);
                    rplSB(sBin.ToString().Substring(0, sBin.ToString().IndexOf(';')));
                    mem_opwds(rpb.ToString(), Scws, vbls);
                    cStmnt.xprn = rpb.ToString();
                    sXList.AddLast(cStmnt);
                    sBin.Remove(0, sBin.ToString().IndexOf(';') + 1);
                    nxtDelim();
                    return cStmnt;
                default:
                    break;
            } // end while switch 
            return cS;
        }
        // end cKwds


        //public void mem_opwds(String rpnStr)
        //{
        //    mem_opwds(rpnStr, cwmem, List < cvbl > cvblsA);
        //}
        public void mem_opwds(String rpnStr, List<uint> Scws, List<Cvbl> cvblsA)
        {
            rop sx = new rop();
            Stack<string> xStk = new Stack<string>();
            StringBuilder sb1 = new StringBuilder(rpnStr);
            StringBuilder sb2 = new StringBuilder();
            string[] strOps = new string[] { " +", " -", "!", "~", "=", "?", ":", "&", "|", "^", " *", "/", "<", ">", "%" };
            string[] xopsA;
            string[] xopnds, xoper;
            string[] wSp = new string[] { };
            while (sb1.Length > 0)
            {
                xopsA = sb1.ToString().Split(expOps, 2, StringSplitOptions.RemoveEmptyEntries);
                xopnds = xopsA[0].Split(wsp, StringSplitOptions.RemoveEmptyEntries);
                sb1.Remove(0, xopsA[0].Length);
                xoper = sb1.ToString().Split(wsp, 2, StringSplitOptions.RemoveEmptyEntries);
                sb1.Remove(0, xoper.Length > 0 ? xoper[0].Length : 0);
                switch (xopnds.Length)
                {
                    case 0: // no opnds, must pop or assign
                        {
                            if (xStk.Count > 0)
                            {
                                sb2.Append(xStk.Pop() + " ");
                                sb2.Append(xoper[0] + " ");
                            }
                            else
                            { // do pop 
                                sb2.Append(xoper.Length > 0 ? xoper[0] + " " : "");
                            }
                        }
                        break;
                    case 1: // one opnd and oper
                        sb2.Append(xopsA[0]);
                        sb2.Append(xoper[0] + " ");
                        break;
                    default: // more than one, push all but 2
                        for (int i = 0; i < xopnds.Length - 2; i++)
                            xStk.Push(xopnds[i]);
                        sb2.Append(xopnds[xopnds.Length - 2] + " " + xopnds[xopnds.Length - 1] + " ");
                        sb2.Append(xoper[0] + " ");
                        break;
                } // end switch opnds.Length
            } // end while(xL < rpnStr.Length)
              //  A = B + (C * D) - (E + F) * G;  C D * B + E F + G * - A =

            while (sb2.Length > 0)
            {
                // C D * B + F + G * F - E = 
                // 4 3 * 2 + 4 - y =  
                // 3 2 * 5 4 * + x =
                //cprec(ref sx);
                //xopsA = sb2.ToString().Split(expOps, 2);  //, StringSplitOptions.RemoveEmptyEntries);
                //sb2.Remove(0, xopsA[0].Length);
                ////sx.oper = sb2.ToString().Substring(xopsA[0].Length, sb2.Length > xopsA[0].Length + 1 ? 2 : 1).Trim();
                //xopsB = xopsA[0].Split(wsp, StringSplitOptions.RemoveEmptyEntries);
                //if (xopsA.Length > 1)
                //{
                //    xopsA = sb2.ToString().Split(wsp, 2);
                //    sx.oper = xopsA[0];
                //    cprec(ref sx);
                //}

                // ftch op2@, op1@, ocw, nxt@; ftch op1,op2,load ocwReg, nxt@; wrt TOS, fth ocw, op2@, rd nxtOp; 
                //                             
                //_|c0|_______________|c0|________________|c0|_______________|c0|_______________|c0|___
                // cwCt = 0	          mem[cwCt.qa]                            mem[cwCt.qa]
                //					                      mem[cwCt.qa + 1]                        mem[cwCt.qa + 1]           
                //										  va[sCw.qb]          vA[op1@, stkCt.qa]
                //										  vb[oCw.qb]          vB[oCw.qb]
                // cwCtA = 2		  cwCtA = nxtScw                                                                    cwCtA = nxtScw


                int tokenIx = 0;
                string[] xA;
                string[] sdelimsB = new string[] { ")", "{", " " };  //  "=", ";", "(", ")", "{", "}", " "
                xA = sb2.ToString().Split(sdelimsB, StringSplitOptions.RemoveEmptyEntries);
                //                mem.Add(get_vbl(xA[x - 2]) << 16 | get_vbl(xA[x - 1]));
                //                mem.Add(sx.opcw << 16 | (sx.oper == "=" ? get_vbl(xA[0]) : 0));

                //                break;
                //            default:
                //                break;
                //        }
                //        vcnt = 0;
                //    }
                //}
                //mem[mem.Count - 1] |= (int)ops.eop << 16;
                //      return;
                tokenIx = sb2.ToString().IndexOfAny(expOps);
                token = tokenIx < 0 ? sb2.ToString() : sb2[tokenIx].ToString();
                xopsA = sb2.ToString().Substring(0, tokenIx).Split(wsp, StringSplitOptions.RemoveEmptyEntries);
                if (xopsA.Length < 2)
                {
                    MessageBox.Show("expected two opnds");
                    return;
                }

                if (token == "=")
                {
                    Scws.Add((uint)(get_vbl(xopsA[1], cvblsA) << 16 | Scws.Count)); //get_vbl(xopsA[1], cvblsA)
                    Scws.Add((uint)((int)(ucdEnum.pway | ucdEnum.eop) << 16 | get_vbl(xopsA[0], cvblsA)));
                    cwDcd.Add(String.Format("{0}" + " ALU to " + xopsA[0], (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0)));
                    sb2.Remove(0, tokenIx + 1);
                    return;
                }
                else
                {
                    Scws.Add((uint)(get_vbl(xopsA[0], cvblsA) << 16 | get_vbl(xopsA[1], cvblsA))); //  Xcws.Count)
                    cwDcd.Add("op1 = " + xopsA[0] + " op2 = " + xopsA[1]);
                }
                sb2.Remove(0, tokenIx + 1);
                do
                {
                    sx.oper = token;
                    cprec(ref sx);
                    tokenIx = sb2.ToString().IndexOfAny(expOps);
                    if (tokenIx < 0)
                    {
                        if (sb2.ToString().TrimStart().Length == 0)
                        {
                            Scws.Add((uint)(sx.opcw | (int)ucdEnum.eop) << 16);
                            cwDcd.Add(String.Format("{0} " + "{1}", (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0), (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x1f)));
                            return;
                        }
                    }
                    token = sb2[tokenIx].ToString();
                    xopsA = sb2.ToString().Substring(0, tokenIx >= 0 ? tokenIx : sb2.Length).Split(wsp, StringSplitOptions.RemoveEmptyEntries);
                    switch (xopsA.Length)
                    {
                        case 0: // back to back opers get opnds from stack
                            Scws.Add((uint)sx.opcw << 16);
                            cwDcd.Add(String.Format("{0} " + "{1}", (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0), (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x1f)));
                            Scws.Add((int)(ucdEnum.pop | ucdEnum.etime) << 16);
                            cwDcd.Add(String.Format("{0}", (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0)));
                            break;
                        case 1: // normal case 1 opnd per oper
                            if (token == "=")
                            {
                                Scws.Add((uint)((sx.opcw | (int)(ucdEnum.pway | ucdEnum.eop)) << 16 | get_vbl(xopsA[0], cvblsA)));
                                cwDcd.Add(String.Format("{0 }" + " {1} " + xopsA[0], (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0), (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x1f)));
                            }
                            else
                            {
                                Scws.Add((uint)(sx.opcw << 16 | get_vbl(xopsA[0], cvblsA)));
                                cwDcd.Add(String.Format("{0} " + "{1} " + xopsA[0], (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0), (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x1f)));
                            }
                            break;
                        case 2: // push alu, get 2 opnds
                                // mem[mem.Count - 2] |= ((int)ops.push);
                            Scws.Add((uint)(sx.opcw | (int)ucdEnum.push) << 16);
                            cwDcd.Add(String.Format("{0}" + "{1}", (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x8f0), (ucdEnum)((Scws[Scws.Count - 1] >> 16) & 0x1f)));
                            Scws.Add((uint)(get_vbl(xopsA[0], cvblsA) << 16 | get_vbl(xopsA[1], cvblsA)));
                            cwDcd.Add("op1 = " + xopsA[0] + " op2 = " + xopsA[1]);
                            break;
                        default: // probably an error, expr could not have more than 2 ?????????
                            break;
                    }
                    sb2.Remove(0, tokenIx + 1);
                }
                while (sb2.ToString().Trim().Length > 0);

                return;
            }
        }

        public string relBld(string xpr)
        {
            StringBuilder orS = new StringBuilder();
            int orix = 0, ssix = 0;
            if ((orix = xpr.Substring(ssix).IndexOf("||")) < 0)
            {
                while (xpr.Length >= ssix)
                {
                    orix = xpr.Substring(ssix).IndexOf("&&");
                    if (orix < 0)
                    {
                        orS.Append("if(" + xpr.Substring(ssix) + ")");
                        break;
                    }
                    else
                        orS.Append(" if(" + xpr.Substring(ssix, orix) + ") ");
                    ssix += orix + 2;
                }
                return orS.ToString();

            }
            else
                {
                    while (xpr.Length >= ssix)
                    {
                        orix = xpr.Substring(ssix).IndexOf("||");
                        if (orix < 0)
                        {
                            orS.Append("else if(" + xpr.Substring(ssix) + ")");
                            break;
                        }
                        else
                            orS.Append(" else if(" + xpr.Substring(ssix, orix) + ") ");
                        ssix += orix + 2;
                    }
                    return orS.ToString();
                }
                //List<string> andS = new List<string>();
                //foreach (string s in orS)
                //{
                //    int andix = 0;
                //    ssix = s.IndexOf("&&");
                //    while (s.Length >= ssix)
                //    {
                //        andix = s.Substring(ssix + 2).IndexOf("&&");
                //        if (andix < 0)
                //        {
                //            andS.Add(s);
                //            break;
                //        }
                //        else
                //            andS.Add(s.Substring(ssix, andix));
                //        ssix += andix + 2;
                //    }
                //    foreach (string s2 in andS)
                //    {
                //        rpb.Remove(0, rpb.Length);
                //        bld_rpl(s2.Trim());
                //    }
                //}
            }

            //    //Choose: 
            //    //if token is an operand, then write it to output. 
            //    //else if token is an operator, then: 
            //    //while ( (token's precedence) ≤ (precedence of the operator on top of the operator-stack) ): 
            //    //pop the top operator from the operator-stack and write it to output. 
            //    //push the token onto the operator-stack. 
            //    //else if token is '(', then push it onto the operator-stack (with precedence -1). 
            //    //else if token is ')', then: 
            //    //while (the top of the operator-stack is not a '(' ): 
            //    //pop the top operator from the operator-stack and write it to output. 
            //    //if the operator-stack becomes empty, then a parentheses-balancing error has occurred. Complain bitterly. 
            //    //pop the '(' off the operator-stack; discard it and the token. 
            //    //else some token error has occurred. Abandon the conversion. 
            //    //Repeat these steps as long as input tokens are available. 
            //    //While (the operator-stack is not empty): 
            //    //pop the top operator from the operator-stack and write it to output. 
            //    //If (the top of the operator-stack is a '(' ), then a parentheses-balancing error has occurred. Complain bitterly. 
            //    //Close the input and the output. 

            //Repeat these steps as long as input tokens are available. 

            //Choose: 
            //if token is an operand, then write it to output. 
            //else if token is an operator, then: 
            //while ( (token's precedence) ≤ (precedence of the operator on top of the operator-stack) ): 
            //pop the top operator from the operator-stack and write it to output. 
            //push the token onto the operator-stack. 
            //else if token is '(', then push it onto the operator-stack (with precedence -1). 
            //else if token is ')', then: 
            //while (the top of the operator-stack is not a '(' ): 
            //pop the top operator from the operator-stack and write it to output. 
            //if the operator-stack becomes empty, then a parentheses-balancing error has occurred. Complain bitterly. 
            //pop the '(' off the operator-stack; discard it and the token. 
            //else some token error has occurred. Abandon the conversion. 
            //Repeat these steps as long as input tokens are available. 
            //While (the operator-stack is not empty): 
            //pop the top operator from the operator-stack and write it to output. 
            //If (the top of the operator-stack is a '(' ), then a parentheses-balancing error has occurred. Complain bitterly. 
            //Close the input and the output. 
            void rplSB(string expr)
            {
                StringBuilder sB = new StringBuilder(expr);  //, rpb = new StringBuilder();
                rplSB(sB);
            }
            void rplSB(StringBuilder sB)
            {
                //string[] spA;
                Stack<rop> opStk = new Stack<rop>();
                rpb.Remove(0, rpb.Length);
                rop sx2;
                string sv;
                int opIx;
                //if (inSB[delimIx] == '=')
                //    if (inSB.ToString().LastIndexOf(';') < 0)
                //    {
                //        sB = new StringBuilder(inSB.ToString());
                //        inSB.Remove(0, inSB.Length);
                //        delimIx = NextLine();
                //while (inSB[delimIx] != ';')
                //{
                //    inSB.Remove(0, inSB.Length);
                //    tokenIx = NextLine();
                //    if ((delimIx = inSB.ToString().LastIndexOf(';')) >= 0)
                //    {
                //        sB.Append(inSB.ToString().Substring(0, delimIx + 1));
                //        inSB.Remove(0, delimIx + 1);
                //        break;
                //    }
                //    else
                //    {
                //        inSB.Remove(0, inSB.Length);
                //    }
                //  sB.Append(inSB.ToString().Substring(0, tokenIx + 1));
                //}
                //}
                rpb.Remove(0, rpb.Length);
                while ((opIx = sB.ToString().IndexOfAny(expOps)) >= 0)
                {
                    //while (inSB.Length > 0 && Char.IsWhiteSpace(inSB[0]))
                    //    inSB.Remove(0, 1);
                    //tokenIx = NextLine(delims);
                    //inSplitA = inSB.ToString().Split(sdelimsA, 2, StringSplitOptions.RemoveEmptyEntries);
                    //token = tokenIx > 0 ? inSplitA[0] : inSB.ToString()[0].ToString();
                    //    switch (inSB[tokenIx])
                    //while (sB.ToString().TrimStart().Length > 0)
                    //{
                    //spA = sB.ToString().Split(expOps, 2, StringSplitOptions.RemoveEmptyEntries);
                    //if (Char.IsLetterOrDigit(sB.ToString().TrimStart()[0]))
                    //{
                    rpb.Append(sB.ToString().Substring(0, opIx).Trim() + " ");
                    sB.Remove(0, opIx);
                    if (sB[0] == ';')
                    {
                        sB.Remove(0, 1);
                        continue;
                    }
                    if (sB[0] == '=')
                    {
                        switch (sB.ToString().Substring(0, 2))
                        {
                            case "==":
                            case "<=":
                            case ">=":
                                break;
                            default:
                                //sB.Remove(spA[0].Length + 1, 1);
                                //sB.Insert(spA[0].Length, " = " + spA[0], 1);
                                break;
                        }
                    }
                    //sB.Remove(0, opIx + 1);
                    //}
                    //else
                    switch (sB.ToString().TrimStart()[0].ToString())
                    {
                        case "(":
                            sx2 = new rop();
                            sx2.prec = -1;
                            sx2.oper = "(";
                            opStk.Push(sx2);
                            sB.Remove(0, 1);
                            break;
                        case ")":
                            while (opStk.Peek().oper != "(")
                                rpb.Append(opStk.Pop().oper + " ");
                            if (opStk.Count == 0)
                                MessageBox.Show("Unbalanced parens");
                            else
                                opStk.Pop();
                            sB.Remove(0, 1);
                            break;
                        default:
                            //while(lower precedence)
                            //    append(pop oper stack)
                            //push token to oper stack
                            // opnd was removed
                            sx2 = new rop();
                            sx2.oper = sB[0].ToString();
                            sB.Remove(0, 1);
                            if (sB.ToString().IndexOfAny(expOps) == 0)
                            {
                                sx2.oper += sB[0];
                                sB.Remove(0, 1);
                            }
                            if (cprec(ref sx2)) // get prec this oper
                                //while ( (token's precedence) ≤ (precedence of the operator on top of the operator-stack) ): 
                                //pop the top operator from the operator-stack and write it to output. 
                                //push the token onto the operator-stack. 
                                while (opStk.Count > 0 && sx2.prec <= opStk.Peek().prec)
                                    rpb.Append(" " + opStk.Pop().oper + " ");
                            else
                                MessageBox.Show("invalid operator");
                            opStk.Push(sx2);
                            break;

                    }
                    while (sB.Length > 0 && Char.IsWhiteSpace(sB[0]))
                        sB.Remove(0, 1);
                }

                //While (the operator-stack is not empty):
                while (opStk.Count > 0)
                {
                    rpb.Append((sv = opStk.Pop().oper) + " ");
                    if (sv == "(")
                        MessageBox.Show("Unbalanced parens");
                    if (opStk.Count > 0 && opStk.Peek().oper == "=")
                    {
                        rpb.Append(rpb.ToString().Substring(0, rpb.ToString().IndexOf(' ')) + " " + (opStk.Pop().oper) + " ");
                        rpb.Remove(0, rpb.ToString().IndexOf(' ') + 1);
                    }
                }
                // A = B + (C * D) - (E + F) * G;
                // B C D * + E F + G * - A =
                return;
            }



            int add_Cvbl(Cvbl cV)
            {
                return add_Cvbl(cV, cvbls);
            }
            int add_Cvbl(Cvbl cV, List<Cvbl> cvblsA)
            {
                int vint;
                if (cV.name.IndexOf(';') > 0)
                    cV.name = cV.name.Substring(0, cV.name.IndexOf(';'));
                Cvbl nv = new Cvbl();
                nv.name = cV.name;
                if (Char.IsDigit(cV.name[0]))
                {
                    if (cV.name.Length > 2 && (cV.name[1] == 'x' || cV.name[1] == 'X'))
                    {
                        if (!int.TryParse(cV.name.Substring(2), NumberStyles.HexNumber, null, out vint))
                            MessageBox.Show("cvbl Parse error");
                        else nv.val = (int)vint;
                    }
                    else
                    {
                        if (!int.TryParse(cV.name, out vint))
                            MessageBox.Show("cvbl Parse error");
                        else nv.val = (int)vint;
                    }
                }
                nv.vix = (int)cvblsA.Count;
                cvblsA.Add(nv);
                return nv.vix;
            }
            int get_vbl(string vbl_nm)
            {
                return get_vbl(vbl_nm, cvbls);
            }

            int get_vbl(string vbl_nm, List<Cvbl> cvblsA)
            {
                int fnix = 0;
                Cvbl cV;
                vbl_nm = vbl_nm.Trim();
                if (vbl_nm.IndexOf(';') > 0)
                    vbl_nm = vbl_nm.Substring(0, vbl_nm.IndexOf(';'));

                if (vbl_nm.IndexOf('.') > 0)
                    if ((fnix = get_vbl(vbl_nm.Substring(0, vbl_nm.IndexOf('.')), cvblsA)) >= 0)
                    {
                        cfn = cvblsA[fnix].cfnref;
                        foreach (Cvbl ne in cfn.cvbls)
                            if (ne.name == vbl_nm)
                                return cfn.cvbls.IndexOf(ne);
                        cV = new Cvbl();
                        cV.name = vbl_nm;
                        return add_Cvbl(cV, cfn.cvbls);
                    }
                foreach (Cvbl v in cvblsA)
                {
                    if (v.name == vbl_nm)
                        return v.vix;
                }
                cV = new Cvbl();
                cV.name = vbl_nm;
                return add_Cvbl(cV, cvblsA);
            }

            private int nxtDelim()
            {
                return nxtDelim(delims);
            }

            private int nxtDelim(char[] srcDeLims)
            {
                string[] cmnts = { "//", "/*", "*/" };
                string nxtln;
                int cmntIx, rtnIx = 0;
                while (!sRdr.EndOfStream) // (rtnIx = sInsb.ToString().IndexOfAny(dLim)) < 0 &&
                {
                    if ((rtnIx = sBin.ToString().IndexOfAny(srcDeLims)) >= 0)
                        break;
                    lineno++;
                    nxtln = sRdr.ReadLine();
                    if ((cmntIx = nxtln.IndexOf("//")) >= 0 || (cmntIx = nxtln.IndexOf("/*")) >= 0)
                        sBin.Append(nxtln.Substring(0, cmntIx));
                    else
                        sBin.Append(nxtln + " ");
                } // end while loop
                rtnIx = sBin.ToString().IndexOfAny(srcDeLims);
                inlineSplit = sBin.ToString().Split(sdelims, 2, StringSplitOptions.RemoveEmptyEntries);
                return rtnIx;
            }

        /*  nxtStmnt gets input using nxtDelim
         *  nxtDelim reads the source file, plan is to use syntax tree instead
         *  nxtStmnt would get nodes from syntax tree instead of parsing input file
         */
        public CStmnt nxtStmnt(ref String token, ref String[] inlineSplit, char[] dLim, List<uint> Scws, List<Cvbl> CsA)
        {
            if (sRdr.EndOfStream && sBin.Length == 0)
                return null;
            switch (sBin[delimIx])
            {
                case '{':  // curly braces surround compound C statements and function bodies
                    {
                        cbcnt++;
                        CStmnt lB = new CStmnt(lineno, "{", "{");
                        llN = new LinkedListNode<CStmnt>(lB);
                        sXList.AddLast(llN);
                        sBin.Remove(0, delimIx + 1);
                        delimIx = nxtDelim();
                        while (sBin[delimIx] != '}')
                        {  // braces are handled recursively
                            nxtStmnt(ref token, ref inlineSplit, delims, Scws, CsA);
                        }
                        cbcnt--;
                        sBin.Remove(0, delimIx + 1);
                        delimIx = nxtDelim();
                        CStmnt rB = new CStmnt(lineno, "}", "}");
                        llN = new LinkedListNode<CStmnt>(rB);
                        sXList.AddLast(llN);
                        return lB;
                    }
                    break;
                case '=':
                    // assignment statements are converted to RPN for evaluation
                    CStmnt xAsgn = new CStmnt(lineno, "=", "=");
                    xAsgn.xprn = sBin.ToString().Substring(0, sBin.ToString().IndexOf(';') + 1).TrimStart();
                    xAsgn.kwd = " = ";
                    //cEx.Add("     " + xAsgn.xprn);
                    rplSB(sBin); // removes ';'
                    mem_opwds(rpb.ToString(), Scws, CsA);
                    delimIx = nxtDelim();
                    return xAsgn;
                    break;
                case '(':  // stmnt kwd, fun def rtn type, fun call name, vbl assign name
                    inlineSplit = sBin.ToString().Substring(0, delimIx).Split(wsp, 2, StringSplitOptions.RemoveEmptyEntries);
                    token = inlineSplit[0]; 
                                           
                    switch (token) // create Node linkedlist and call cKwds to gen pgm kwds
                    {
                        case "if":
                            CIf cIf = new CIf(lineno, sBin.ToString().Substring(0, delimIx + 1), token);
                            llN = new LinkedListNode<CStmnt>(cIf);
                            sXList.AddLast(llN);
                            sBin.Remove(0, delimIx + 1);
                            cS = cIf;
                            cKwds(sXList, cvbls, Scws);
                            break;
                        case "for":
                            sBin.Remove(0, delimIx + 1);
                            delimIx = nxtDelim(rparen);
                            if (sBin[delimIx] != ')')
                                return null;
                            inSplitA = sBin.ToString().Substring(0, delimIx).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                           
                            CFor cFor = new CFor(lineno, '(' + inSplitA[1] + ')', token);
                            cFor.xprn = inSplitA[0];  // save pre/cond/post strings and advance to "for" body
                            cFor.post = inSplitA[2];
                            sBin.Remove(0, delimIx + 1);
                            delimIx = nxtDelim();
                            sXList.AddLast(cFor);
                            cS = cFor;
                            cKwds(sXList, cvbls, Scws);
                            break;
                        case "while":
                            sBin.Remove(0, delimIx);
                            delimIx = nxtDelim(rparen);
                            if (sBin[delimIx] != ')')
                                return null;
                            CWhile cWhile = new CWhile(lineno, sBin.ToString().Substring(0, delimIx + 1), token);
                            sBin.Remove(0, delimIx + 1);
                            llN = new LinkedListNode<CStmnt>(cWhile);
                            sXList.AddLast(llN);
                            cS = cWhile;
                            cKwds(sXList, cvbls, Scws);
                            break;
                        case "do":
                            //                                    cvbl cV = new cvbl();
                            //                                    cV.type = inlineSplit[0];
                            //                                    inSB.Remove(0, inSB.ToString().IndexOf(inlineSplit[1]));
                            //                                    tokenIx = NextLine();
                            //                                    cV.name = inlineSplit[0];
                            //                                    //cvblsA[fid].type = type;
                            //                                    //cvblsA[fid].name = inlineSplit[0];
                            //                                    inSB.Remove(0, tokenIx);
                            //                                    tokenIx = NextLine();
                            break;
                        case "switch":
                            CSwitch cSwitch = new CSwitch(lineno, sBin.ToString().Substring(0, delimIx + 1), token);
                            sBin.Remove(0, delimIx + 1);
                            sXList.AddLast(cSwitch);
                            cS = cSwitch;
                            break;
                        case "int":
                            Cvbl cV = new Cvbl();
                            cV.type = token;
                            cV.name = inlineSplit[1];
                            if (sBin[delimIx] == '(')
                                sBin.Remove(0, delimIx + 1);
                            else { }
                            delimIx = nxtDelim();
                            if (sBin[delimIx] == ')')
                            {
                                cfuns.Add(cfn = new CFunc());
                                cfn.name = cV.name;
                                cfn.type = cV.type;
                                cV.cfnref = cfn;
                                cV.vix = add_Cvbl(cV, cvbls);
                                cvbls[cV.vix].cfnref = cfn;
                                inSplitA = sBin.ToString().Substring(0, delimIx).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string s in inSplitA)  // setup assigns for args
                                {
                                    Cvbl v = new Cvbl();
                                    v.type = s.Split(wsp, StringSplitOptions.RemoveEmptyEntries)[0];
                                    v.name = (s.Split(wsp, StringSplitOptions.RemoveEmptyEntries)[1]);  // cfn.name + "." + 
                                    v.vix = cfn.cvbls.Count;
                                    cfn.cvbls.Add(v);
                                }
                                sBin.Remove(0, delimIx + 1);
                                delimIx = nxtDelim();
                                int fncbct = 0;
                                while ((delimIx = nxtDelim()) >= 0)
                                {
                                    switch (sBin[delimIx].ToString())
                                    {
                                        case "{":
                                            fncbct++;
                                            cfn.sb.Append(sBin.ToString().Substring(0, delimIx + 1));
                                            sBin.Remove(0, delimIx + 1);
                                            break;
                                        case "}":
                                            fncbct--;
                                            cfn.sb.Append(sBin.ToString().Substring(0, delimIx + 1));
                                            sBin.Remove(0, delimIx + 1);
                                            break;
                                        default:
                                            cfn.sb.Append(sBin.ToString().Substring(0, delimIx + 1));
                                            sBin.Remove(0, delimIx + 1);
                                            break;
                                    }
                                    if (fncbct == 0)
                                    {
                                        delimIx = nxtDelim();
                                        break;
                                    }
                                }
                                break;
                            }
                            else { } // expected '('
                            break;
                        default:
                            CFunc fn = cvbls[get_vbl(token, cvbls)].cfnref;
                            //inSB.Remove(0, inSB.ToString().IndexOf(inlineSplit[1]));
                            delimIx = nxtDelim();
                            if (sBin[delimIx] == '(')
                                sBin.Remove(0, delimIx + 1);
                            delimIx = nxtDelim();
                            if (sBin[delimIx] == ')')
                            {
                                inSplitA = sBin.ToString().Substring(0, delimIx).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                List<string> argL = new List<string>(inSplitA);
                                argL.Reverse();
                                if (argL.Count > 0)
                                    Scws.Add((uint)get_vbl(argL[0], cvbls));
                                Scws.Add((uint)(ucdEnum.call | ucdEnum.etime | ucdEnum.push) << 16);
                                argL.RemoveAt(0);
                                foreach (string s in argL) // fun cws in fun memory and added to cws at eof
                                {   // fun call pushes args then goes to fun cws, call list has ix of calls updated when cws added at EOF
                                    Scws[Scws.Count - 1] |= (uint)get_vbl(s.Trim(), cvbls);
                                    Scws.Add((uint)(ucdEnum.call | ucdEnum.etime) << 16);
                                }
                                fn.calls.Add(Scws.Count - 1);
                                Scws[Scws.Count - 1] = ((uint)(ucdEnum.call | ucdEnum.etime | ucdEnum.eop) << 16);
                                sBin.Remove(0, delimIx + 1);
                            }
                            sBin.Remove(0, sBin.ToString().IndexOf(';') + 1);
                            delimIx = nxtDelim();
                            break;
                    }   // end kwd/token switch
                    break;
            }  // end delim switch
            return cS;
        }  // end nxtStmnt



        //public void scan(string name, ref int scn_ix, int val, long seek_ch,int  rn_tm)
        //{
        //    int ln_tm = rn_tm;
        //    if (scn.Count > 0 && scn[(int)scn_ix].name.Equals(name))
        //    {
        //        s_p = scn[(int)scn_ix];
        //    }
        //    else
        //    {
        //        s_p = new scrn();
        //        s_p.ln = new string('.', ln_tm);
        //        s_p.ln += (val > 0) ? "/" : "\\";
        //        s_p.tm = (int)rn_tm;
        //        vals vt = new vals();
        //        vt.time = rn_tm;
        //        vt.val = val;
        //        scn_ix = scn.Count;
        //        s_p.name = name;
        //        s_p.scn_ix = scn.Count;
        //        s_p.trace = new List<vals>();
        //        s_p.trace.Add(vt);
        //        scn.Add(s_p);
        //        return;
        //    }
        //    if (s_p.ln.Length < ln_tm)
        //        s_p.ln = s_p.ln.PadRight(ln_tm, s_p.ln[s_p.ln.Length - 1] == '/' ? '+' : '_');
        //    s_p.ln += (val != 0 ? '/' : '\\');
        //    s_p.tm = ln_tm;
        //    vals vt2 = new vals();
        //    vt2.time = rn_tm;
        //    vt2.val = val;
        //    s_p.trace.Add(vt2);
        //    s_p.seek_pos = seek_ch;
        //    scn[(int)scn_ix] = s_p;
        //    return;
        //}

        //char scn_out(ref byte mode, int ln_tm, ListBox pLBox, ref byte pCode)
        //{
        //    int nssAr = 0;
        //    int units;
        //    int tens;
        //    int tens_ct;
        //    int length;
        //    String lbadd = "**Double click facility for first change then Button2 for next change**";
        //    lB2.Items.Add(lbadd);
        //    nssAr++;
        //    tens = scn_tm % 100;
        //    units = tens % 10;
        //    tens /= 10;
        //    byte cTens = (byte)tens;
        //    String sLine;
        //    String sChar;
        //    byte[] sCh = new byte[128];
        //    lbadd = "**Time**   ";
        //    lbadd += scn_tm;
        //    lB2.Items.Add(lbadd);
        //    nssAr++;
        //    sLine = "                    ";
        //    for (tens_ct = units, length = 0; length < 100; length++)
        //    {
        //        sChar = String.Format("{0}", tens);
        //        sLine += sChar;
        //        if (tens_ct++ == 9)
        //        {
        //            if (tens++ == 9) tens = 0;
        //            tens_ct = 0;
        //        }
        //    }
        //    lB2.Items.Add(sLine);
        //    sLine = "                    ";
        //    for (length = 0; length < 100; length++)
        //    {
        //        sChar = String.Format("{0}", units);
        //        sLine += sChar;
        //        if (units++ == 9) units = 0;
        //    }
        //    lB2.Items.Add(sLine);
        //    string lno;
        //    foreach (scrn so in scn)
        //    {
        //        lno = so.ln;
        //        if (lno.Length < ln_tm)
        //        {
        //            if (lno[lno.Length - 1] == '\\') lno += '.';
        //            if (lno[lno.Length - 1] == '/') lno += '-';
        //        }
        //        if (so.name != null)
        //        {
        //            lbs = String.Format("{0}{1}", so.name.PadRight(20, '.'), lno.PadRight(ln_tm, lno[lno.Length - 1]));
        //            lB2.Items.Add(lbs);
        //        }
        //    }

        //    return 'l';
        //}
        // end scan

        public class Cvbl
            {
                public int val
                {
                    get { return vval; }
                    set { vval = value; }
                }
                public CFunc cfnref;
                public string name; public int vval; public string type;
                public int scn_ix; public int vix = -1;
            }


        public class CFunc
        {
            public string name; public int vval; public string type;
            public int scn_ix; public int vix = unchecked((int)-1);
            //public List<int> parms = new List<int>();
            public List<string> args = new List<string>();
            public List<string> locals = new List<string>();
            public List<uint> cwds = new List<uint>();
            public List<Cvbl> cvbls = new List<Cvbl>(1024);
            public LinkedList<CStmnt> sList = new LinkedList<CStmnt>();
            public int cwix;
            public int mcnt;
            public int vcnt;
            public List<int> calls = new List<int>();
            public StringBuilder sb = new StringBuilder();
        }

        public List<CFunc> cfuns = new List<CFunc>();

        private void HexWrt(String xName, List<int> inList)
        {
            //string hexout;
            StreamWriter mySW = new StreamWriter(xName, false, Encoding.ASCII);
            StringBuilder rsb = new StringBuilder();
            uint ipt = 0;
            int chksum = 0, haddr = 0;
            string hexout, hzero = "00000000";
            //string[]inSplit;

            for (int aX = 0; aX < inList.Count; aX++)
            {
                ipt = (uint)inList[aX];
                hexout = String.Format("{0:X}", ipt);
                rsb.Append(hzero.Substring(0, 8 - hexout.Length) + hexout);
                hexout = String.Format("{0:X}", haddr);
                rsb.Insert(0, "04");
                rsb.Insert(2, hzero.Substring(0, 4 - hexout.Length) + hexout);
                rsb.Insert(6, "00");    //: 03 00 3000 02337A 1E
                for (int csbx = 0; rsb.Length - 2 >= csbx; csbx += 2)
                {
                    chksum += Int32.Parse(rsb.ToString().Substring(csbx, 2), NumberStyles.HexNumber);
                }
                chksum &= 0x000000ff;
                chksum ^= 0x000000ff;
                chksum += 1;
                chksum &= 0x000000ff;
                hexout = String.Format("{0:X}", chksum);
                rsb.Append(hzero.Substring(0, 2 - hexout.Length) + hexout);
                // rsb.Append(":00000001FF");
                mySW.WriteLine(":" + rsb.ToString());
                rsb.Remove(0, rsb.Length);
                chksum = 0;
                haddr++;
            }
            mySW.WriteLine(":00000001FF");
            mySW.Flush();
            mySW.Close();
            haddr = 0;
            return;
        }
        // End HexWrt

        private void makCond(String cond, List<Cvbl> vbls, List<uint> Scws)
            {
                if (cond.IndexOf("&&") > 0 || cond.IndexOf("||") > 0)
                    relBld(cond);
                else
                    mem_opwds(cond, Scws, vbls);
            }
  // end cParse

        [Flags]
        public enum ucdEnum
        {
            //      
            call = 0x8000,  // 
            rtn = 0x4000,  // 
            tna = 0x2000,  // 0x000E combines with cond opers and uses spare opcodes
            fna = 0x1000,  // 0x000F
            eop = 0x0800,  //
            gtr = 0x0410,
            eql = 0x0210,  // !eql = 0x0510
            less = 0x0110,
            pway = 0x0080,  // 
            push = 0x0050,  // may use 0x8040 to stack call parms, or with ALU codes 
            pop = 0x0030,  // 
            etime = 0x0010,  // part of opcode for ALU and compares. decoded also for sequencing
            nop = 0x0000, bnd = 0x0011, bxo = 0x0012, bor = 0x0013, add = 0x0014, sub = 0x0015,
            mpy = 0x0016, dvd = 0x0017, mlo = 0x0018, lsf = 0x0019, rsf = 0x001A, ldm = 0x000E, stm = 0x000F
        }

        enum prec { eq = 1, qm, oo, nn, o, xo, n, ee, lg, srl, pm, md };

        public bool cprec(ref rop sx)
        {

            switch (sx.oper)
            {
                case "&": sx.prec = (int)prec.n; sx.opcw = (int)ucdEnum.bnd; return true;
                case "|": sx.prec = (int)prec.o; sx.opcw = (int)ucdEnum.bor; return true;
                case "^": sx.prec = (int)prec.xo; sx.opcw = (int)ucdEnum.bxo; return true;
                case "+": sx.prec = (int)prec.pm; sx.opcw = (int)ucdEnum.add; return true;
                case "-": sx.prec = (int)prec.pm; sx.opcw = (int)ucdEnum.sub; return true;
                //case "&&": //    sx.prec = (int)prec.nn; //    sx.opcw = (int)ops.NN; //    return true;
                //case "||": //    sx.prec = (int)prec.oo; //    sx.opcw = (int)ops.OO; //    return true;
                case ">": sx.prec = (int)prec.lg; sx.opcw = (int)ucdEnum.gtr; return true;
                case "<": sx.prec = (int)prec.lg; sx.opcw = (int)ucdEnum.less; return true;
                case "!=": sx.prec = (int)prec.ee; sx.opcw = (int)(ucdEnum.less | ucdEnum.gtr); return true; // < or > is !=
                case "<=": sx.prec = (int)prec.ee; sx.opcw = (int)(ucdEnum.less | ucdEnum.eql); return true;
                case "==": sx.prec = (int)prec.ee; sx.opcw = (int)ucdEnum.eql; return true;
                case ">=": sx.prec = (int)prec.ee; sx.opcw = (int)(ucdEnum.gtr | ucdEnum.eql); return true;
                case "*": sx.prec = (int)prec.md; sx.opcw = (int)ucdEnum.mpy; return true;
                case "/": sx.prec = (int)prec.md; sx.opcw = (int)ucdEnum.dvd; return true;
                case "%": sx.prec = (int)prec.md; sx.opcw = (int)ucdEnum.mlo; return true;
                case "<<": sx.prec = (int)prec.srl; sx.opcw = (int)ucdEnum.lsf; return true;
                case ">>": sx.prec = (int)prec.srl; sx.opcw = (int)ucdEnum.rsf; return true;
                case "(": sx.prec = -1; return false;
                case "=": sx.prec = -1; sx.opcw = (int)ucdEnum.pway; return true;
                default: MessageBox.Show("Missed op "); return false;
            }
        }  // end cprec
        public void memInit()
        {
            if (MessageBox.Show("Wrt new hex files?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            List<int> regs = new List<int>();
            foreach (Cvbl v in cvbls)
            {
                regs.Add(v.val);
            }
            HexWrt("RegRam.hex", regs);
        }

    }
    public class CStmnt
    {
        public int cwix, srcln, loopix;
        public string name, xprn, kwd, cond;

        public CStmnt()
        { }
        public CStmnt(int lno, String x, String k)
        {
            srcln = lno;
            cond = x;
            xprn = x;
            kwd = k.Trim();
        }
    }
    public class CIf : CStmnt
    {
        public CIf(int lno, String x, String k) : base(lno, x, k) { }
        public string cond { get { return base.cond; } set { base.cond = value; } }
        public LinkedList<CStmnt> tList = new LinkedList<CStmnt>();
        public LinkedList<CStmnt> fList = new LinkedList<CStmnt>();
        //public int condix;
        public int tcx;
        public int fcx;
        public int enx;
        public String ckwd { get { return base.kwd; } set { base.kwd = value.Trim(); } }
        public String lineno { get { return base.srcln.ToString(); } set { base.srcln = Int32.Parse(value); } }
    }
    public class CFor : CStmnt
    {
        public CFor(int lno, String x, String k) : base(lno, x, k) { }
        public List<CStmnt> preAsgn = new List<CStmnt>();
        public List<CStmnt> postAsgn = new List<CStmnt>();
        public string init, post;
        public string cond { get { return base.cond; } set { base.cond = value; } }
        public LinkedList<CStmnt> fList = new LinkedList<CStmnt>();
        public CStmnt initref;
        public CStmnt tref;
        public CStmnt endref;
        public int condix;
        public int bodyix;
        public int loopix { get { return base.loopix; } set { base.loopix = value; } }
        public int tcx;
        public int fcx;
        public String ckwd { get { return base.kwd; } set { base.kwd = value.Trim(); } }
        public String lineno { get { return base.srcln.ToString(); } set { base.srcln = Int32.Parse(value); } }
    }
    public class CWhile : CStmnt
    {
        public CWhile(int lno, String x, String k) : base(lno, x, k) { }
        public string ckwd { get { return base.kwd; } set { base.kwd = value.Trim(); } }
        public string cond { get { return base.cond; } set { base.cond = value; } }
        public string xprn { get { return base.xprn; } set { base.xprn = value; } }
        public int lineno { get { return base.srcln; } set { base.srcln = value; } }
        public int loopix, condix, bodyix, endix; // Cengine
    }
    public class CSwitch : CStmnt
    {
        public CSwitch(int lno, String x, String k) : base(lno, x, k) { }
        public List<CStmnt> preAsgn = new List<CStmnt>();
        public List<CStmnt> postAsgn = new List<CStmnt>();
        public string cond;
        public String ckwd { get { return base.kwd; } set { base.kwd = value.Trim(); } }
        public int lineno { get { return base.srcln; } set { base.srcln = value; } }

    }


    //public CStmnt nxtStmnt(char[] dLim, List<uint> Scws, List<uint> Xcws, List<cvbl> cvblsA)
    //{
    //    CStmnt lpS;
    //   // NextLine(delims); // gets assigns, loops, control, compound statements
    //    while ((tokenIx = NextLine()) >= 0)
    //    {
    //        //while (inSB.Length > 0 && Char.IsWhiteSpace(inSB[0]))
    //        //    inSB.Remove(0, 1);
    //        //tokenIx = NextLine(delims);
    //        //inSplitA = inSB.ToString().Split(sdelimsA, 2, StringSplitOptions.RemoveEmptyEntries);
    //        //token = tokenIx > 0 ? inSplitA[0] : inSB.ToString()[0].ToString();
    //        switch (inSB[tokenIx])
    //        {
    //            case '=':
    //                //if(inSB.ToString().IndexOf(';') < 0)
    //                //{
    //                //    rpb = new StringBuilder(inSB.ToString());
    //                //    while (rpb.ToString().IndexOf(';') < 0)
    //                //    {
    //                //        inSB.Remove(0, inSB.Length);
    //                //        tokenIx = NextLine(semic);
    //                //        rpb.Append(inSB.ToString());
    //                //    }
    //                // //   inSB = new StringBuilder(rpb.ToString());
    //                //}
    //                rplSB(inSB);
    //                mem_opwds(rpb.ToString(), Scws, Xcws, cvblsA);
    //                break;
    //            case '(':
    //                token = inlineSplit[0].Trim();
    //                switch (token)
    //                {
    //                    case "if":
    //                    case "for":
    //                    case "while":
    //                    case "switch":
    //                        if (inSB[tokenIx] == '(')
    //                        inSB.Remove(0, tokenIx +1);
    //                        tokenIx = NextLine();
    //                        if (inSB[tokenIx] == ')')
    //                        {
    //                            inSB.Insert(0, '(');
    //                            cKwds(sXList, cvblsA, Scws, Xcws);
    //                        }
    //                        break;
    //                    case "main":
    //                        //NextLine();
    //                        CStmnt smain = new CStmnt();
    //                        smain.name = "main";
    //                        cvbl cv = new cvbl();
    //                        cv.name = "0";
    //                        cv.vix = add_cvbl(cv);
    //                        cv = new cvbl();
    //                        cv.name = "1";
    //                        cv.vix = add_cvbl(cv);
    //                        cv = new cvbl();
    //                        cv.name = "NOS";
    //                        cv.vix = add_cvbl(cv);
    //                        fmain = new cFunc();
    //                        fmain.name = "main";
    //                        sXList.AddLast(smain);
    //                        if (inSB[tokenIx] == '(')
    //                            inSB.Remove(0, tokenIx + 1);
    //                        tokenIx = NextLine();
    //                        if (inSB.ToString().TrimStart()[0] == ')') // main uses no args
    //                            inSB.Remove(0, tokenIx + 1);  // check for no args ?? 
    //                        else
    //                        {
    //                            MessageBox.Show(" main syntax");
    //                            return null;
    //                        }
    //                        break;
    //                    default:
    //                        if (inlineSplit[0] == "do")
    //                        {
    //                            inSB.Remove(0, inSB.ToString().IndexOf(inlineSplit[1]));
    //                            tokenIx = NextLine();
    //                            cKwds(sXList, cvblsA, Scws, Xcws);
    //                            break;

    //                        }
    //                                if (inlineSplit[0] == "int")
    //                                {
    //                                    cvbl cV = new cvbl();
    //                                    cV.type = inlineSplit[0];
    //                                    inSB.Remove(0, inSB.ToString().IndexOf(inlineSplit[1]));
    //                                    tokenIx = NextLine();
    //                                    cV.name = inlineSplit[0];
    //                                    //cvblsA[fid].type = type;
    //                                    //cvblsA[fid].name = inlineSplit[0];
    //                                    inSB.Remove(0, tokenIx);
    //                                    tokenIx = NextLine();
    //                                    if (inSB[tokenIx] == '(')
    //                                    {
    //                                        inSB.Remove(0, tokenIx + 1);
    //                                        cfuns.Add(cfn = new cFunc());
    //                                        cfn.name = cV.name;
    //                                        cfn.type = cV.type;
    //                                        cV.cfnref = cfn;
    //                                        cV.vix = add_cvbl(cV, cvbls);
    //                                        cvbls[cV.vix].cfnref = cfn;
    //                                    }
    //                                    else { } // expected '('
    //                                    tokenIx = NextLine();
    //                                    if(inSB[tokenIx] == ')')
    //                                    {
    //                                        inSplitA = inSB.ToString().Substring(0, tokenIx).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    //                                        foreach (string s in inSplitA)  // setup assigns for args
    //                                        {
    //                                            cvbl v = new cvbl();
    //                                            v.type = s.Split(wsp, StringSplitOptions.RemoveEmptyEntries)[0];
    //                                            v.name = (s.Split(wsp, StringSplitOptions.RemoveEmptyEntries)[1]);  // cfn.name + "." + 
    //                                            v.vix = cfn.cvbls.Count;
    //                                            cfn.cvbls.Add(v);
    //                                        }
    //                                        inSB.Remove(0, tokenIx + 1);
    //                                        //    tokenIx = NextLine();
    //                                        int fncbct = 0;
    //                                        while ((tokenIx = NextLine()) >= 0)
    //                                        {
    //                                            //ckwds(cfn.sList, );
    //                                            tokenIx = NextLine();
    //                                            inSplitA = inSB.ToString().Split(delims, 2);
    //                                            token = inSB[inSplitA[0].Length].ToString();

    //                                            switch (inSB[tokenIx].ToString())
    //                                            {
    //                                                case "{":
    //                                                    fncbct++;
    //                                                    cfn.sb.Append(inSB[tokenIx]);
    //                                                    inSB.Remove(0, tokenIx + 1);
    //                                                    break;
    //                                                case "}":
    //                                                    fncbct--;
    //                                                    cfn.sb.Append(inSB[tokenIx]);
    //                                                    inSB.Remove(0, tokenIx + 1);
    //                                                    break;
    //                                                default:
    //                                                    // tokenIx = NextLine();
    //                                                    cfn.sb.Append(inSB.ToString().Substring(0, tokenIx + 1));
    //                                                    inSB.Remove(0, tokenIx + 1);
    //                                                    break;
    //                                            }
    //                                            if (fncbct == 0)
    //                                            {
    //                                                //inSB.Insert(0, cfn.sb.ToString());
    //                                                //nxtTkn(delims, cfn.cwds, cfn.cvbls);
    //                                                //cfn.cwds[cfn.cwds.Count - 1] |= (uint)(ops.call | ops.rtn) << 16;
    //                                                //if (((ops)(cfn.cwds[cfn.cwds.Count - 1] >> 16) & ops.eop) == ops.eop)
    //                                                {

    //                                                }
    //                                                //cfn.cwds.Add(0);
    //                                                break;
    //                                            }
    //                                        }
    //                                    }
    //                                }
    //                                else
    //                                {
    //                                    token = "fncall";
    //                                    cFunc fn = cvbls[get_vbl(inlineSplit[0], cvbls)].cfnref;
    //                                    inSB.Remove(0, inSB.ToString().IndexOf(inlineSplit[1]));
    //                                    tokenIx = NextLine();
    //                                    if (inSB[tokenIx] == ')')
    //                                    {
    //                                        inSplitA = inSB.ToString().Substring(0, tokenIx).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    //                                        List<string> argL = new List<string>(inSplitA);
    //                                        argL.Reverse();
    //                                        if (argL.Count > 0)
    //                                            Scws.Add((uint)get_vbl(argL[0], cvbls));
    //                                        Scws.Add((uint)(ops.call | ops.etime | ops.push) << 16);
    //                                        argL.RemoveAt(0);
    //                                        foreach (string s in argL) // fun cws in fun memory and added to cws at eof
    //                                        {   // fun call pushes args then goes to fun cws, call list has ix of calls updated when cws added at EOF
    //                                            Scws[Scws.Count - 1] |= (uint)get_vbl(s.Trim(), cvbls);
    //                                            Scws.Add((uint)(ops.call | ops.etime) << 16);
    //                                        }
    //                                        fn.calls.Add(Scws.Count - 1);
    //                                        Scws[Scws.Count - 1] = ((uint)(ops.call | ops.etime | ops.eop) << 16);
    //                                        inSB.Remove(0, tokenIx + 1);
    //                                    }
    //                                    inSB.Remove(0, inSB.ToString().IndexOf(';') + 1);
    //                                    //if (fn.cwix == 0)
    //                                    //    fn.cwix = mem.Count + 1;
    //                                    //mem.AddRange(fn.cwds);
    //                                    //mem[mem.Count - 1] |= (uint)(ops.call | ops.rtn) << 16;
    //                                    cwDcd.Add(String.Format("{0} arg pway", (ops)((Scws[Scws.Count - 1] >> 16) & 0x8f0)));
    //                                }
    //                            }
    //                        tokenIx = NextLine();
    //                        break;
    //                }
    //                break;
    //            default:
    //                break;
    //        }
    //    }
    //    return sXList.First.Value;
    //}  // end nxttkn
    ////scrn s_p;

    //public List<scrn> scn = new List<scrn>();

    //void rplSB(string expr)
    //{
    //    StringBuilder sB = new StringBuilder(expr);  //, rpb = new StringBuilder();
    //    rplSB(sB);
    //}
    //void rplSB(StringBuilder sB)
    //{
    //    string[] spA;
    //    Stack<rop> opStk = new Stack<rop>();
    //    rpb.Remove(0, rpb.Length);
    //    rop sx2;
    //    string sv;

    //    while (sB.ToString().TrimStart().Length > 0)
    //    {
    //        spA = sB.ToString().Split(expOps, 2, StringSplitOptions.RemoveEmptyEntries);
    //        if (Char.IsLetterOrDigit(sB.ToString().TrimStart()[0]))
    //        {
    //            rpb.Append(spA[0].Trim() + " ");
    //            if (sB.Length > spA[0].Length + 1 && sB[spA[0].Length + 1] == '=')
    //            {
    //                switch (sB.ToString().Substring(spA[0].Length, 2))
    //                {
    //                    case "==":
    //                    case "<=":
    //                    case ">=":
    //                        break;
    //                    default:
    //                        sB.Remove(spA[0].Length + 1, 1);
    //                        sB.Insert(spA[0].Length, " = " + spA[0], 1);
    //                        break;
    //                }
    //            }
    //            sB.Remove(0, spA[0].Length);
    //        }
    //        else
    //            switch (sB.ToString().TrimStart()[0].ToString())
    //            {
    //                case "(":
    //                    sx2 = new rop();
    //                    sx2.prec = -1;
    //                    sx2.oper = "(";
    //                    opStk.Push(sx2);
    //                    sB.Remove(0, 1);
    //                    break;
    //                case ")":
    //                    while (opStk.Peek().oper != "(")
    //                        rpb.Append(opStk.Pop().oper + " ");
    //                    if (opStk.Count == 0)
    //                        MessageBox.Show("Unbalanced parens");
    //                    else
    //                        opStk.Pop();
    //                    sB.Remove(0, 1);
    //                    break;
    //                default:
    //                    //while(lower precedence)
    //                    //    append(pop oper stack)
    //                    //push token to oper stack
    //                    // opnd was removed
    //                    sx2 = new rop();
    //                    sx2.oper = sB[0].ToString();
    //                    sB.Remove(0, 1);
    //                    if (sB.ToString().IndexOfAny(expOps) == 0)
    //                    {
    //                        sx2.oper += sB[0];
    //                        sB.Remove(0, 1);
    //                    }
    //                    if (cprec(ref sx2)) // get prec this oper
    //                        //while ( (token's precedence) ≤ (precedence of the operator on top of the operator-stack) ): 
    //                        //pop the top operator from the operator-stack and write it to output. 
    //                        //push the token onto the operator-stack. 
    //                        while (opStk.Count > 0 && sx2.prec <= opStk.Peek().prec)
    //                            rpb.Append(" " + opStk.Pop().oper + " ");
    //                    else
    //                        MessageBox.Show("invalid operator");
    //                    opStk.Push(sx2);
    //                    break;

    //            }
    //        while (sB.Length > 0 && Char.IsWhiteSpace(sB[0]))
    //            sB.Remove(0, 1);
    //    }

    //    //While (the operator-stack is not empty):
    //    while (opStk.Count > 0)
    //    {
    //        rpb.Append((sv = opStk.Pop().oper) + " ");
    //        if (sv == "(")
    //            MessageBox.Show("Unbalanced parens");
    //        if (opStk.Count > 0 && opStk.Peek().oper == "=")
    //        {
    //            rpb.Append(rpb.ToString().Substring(0, rpb.ToString().IndexOf(' ')) + " " + (opStk.Pop().oper) + " ");
    //            rpb.Remove(0, rpb.ToString().IndexOf(' ') + 1);
    //        }
    //    }
    //    // A = B + (C * D) - (E + F) * G;
    //    // B C D * + E F + G * - A =
    //    return;
    //}

    //private int nxtDelim()
    //{
    //    return nxtDelim(delims);
    //}

    //private int nxtDelim(char[] srcDeLims)
    //{
    //    string nxtln;
    //    int cmntIx, rtnIx = 0;
    //    while (!sRdr.EndOfStream) // (rtnIx = sInsb.ToString().IndexOfAny(dLim)) < 0 &&
    //    {
    //        rtnIx = inSB.ToString().IndexOfAny(srcDeLims);
    //        if (rtnIx >= 0)
    //            break;
    //        lineno++;
    //        nxtln = sRdr.ReadLine();
    //        if ((cmntIx = nxtln.IndexOf("//")) >= 0 || (cmntIx = nxtln.IndexOf("/*")) >= 0)
    //            inSB.Append(nxtln.Substring(0, cmntIx));
    //        else
    //            inSB.Append(nxtln.Trim() + " ");
    //    } // end while loop
    //    rtnIx = inSB.ToString().IndexOfAny(srcDeLims);
    //    //inlineSplit = inSB.ToString().Split(sdelims, 2, StringSplitOptions.RemoveEmptyEntries);
    //    return rtnIx;
    //}

}

