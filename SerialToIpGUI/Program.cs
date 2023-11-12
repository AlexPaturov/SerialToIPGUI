// Decompiled with JetBrains decompiler
// Type: SerialToIpGUI.Program
// Assembly: SerialToIPGUI, Version=1.9.5866.19643, Culture=neutral, PublicKeyToken=null
// MVID: B1D67A1F-B8BA-49F3-B87A-0CFB4BE0BA84
// Assembly location: C:\Users\Alex\Downloads\SerialToIPGUI_v1.9_2016-01-23\SerialToIPGUI.exe

using System;
using System.Windows.Forms;

namespace SerialToIpGUI
{
  internal sealed class Program
  {
    [STAThread]
    private static void Main(string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run((Form) new MainForm());
    }
  }
}
