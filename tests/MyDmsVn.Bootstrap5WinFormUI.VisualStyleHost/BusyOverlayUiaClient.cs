using System;
using System.Text;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

// Test-only out-of-process native UIA client; no provider/filter is installed in the application.
public static class BusyOverlayUiaClient
{
    public static string Command(IntPtr handle)
    {
        // ABI: https://github.com/microsoft/win32metadata/blob/9cf94833626504b330c2f616ce71a2d3497cdc76/generation/WinSDK/RecompiledIdlHeaders/um/UIAutomationClient.h
        // IUIAutomationElement::GetCurrentPattern and IUIAutomationInvokePattern::Invoke.
        const string source = @"
using System;
using System.Runtime.InteropServices;
public static class NativeUiaInvoke {
 [DllImport(""ole32.dll"")] static extern int CoCreateInstance(ref Guid cls, IntPtr outer, uint context, ref Guid iid, out IntPtr value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int ElementFromHandle(IntPtr self, IntPtr hwnd, out IntPtr element);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetPattern(IntPtr self, int pattern, out IntPtr value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetProperty(IntPtr self, int property, [MarshalAs(UnmanagedType.Struct)] out object value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Invoke(IntPtr self);
 static T Method<T>(IntPtr value, int slot) { return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(value), slot * IntPtr.Size), typeof(T)); }
 public static void Run(long hwnd) {
  IntPtr automation=IntPtr.Zero, element=IntPtr.Zero, pattern=IntPtr.Zero, invoke=IntPtr.Zero;
  try {
   var cls=new Guid(""ff48dba4-60ef-4201-aa87-54103eef594e""); var iid=new Guid(""30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"");
   Marshal.ThrowExceptionForHR(CoCreateInstance(ref cls, IntPtr.Zero, 1, ref iid, out automation));
   Marshal.ThrowExceptionForHR(Method<ElementFromHandle>(automation,6)(automation,new IntPtr(hwnd),out element));
   object type, name, provider;
   Marshal.ThrowExceptionForHR(Method<GetProperty>(element,10)(element,30003,out type));
   Marshal.ThrowExceptionForHR(Method<GetProperty>(element,10)(element,30005,out name));
   Marshal.ThrowExceptionForHR(Method<GetProperty>(element,10)(element,30107,out provider));
   Console.WriteLine(""Native UIA: name=""+name+""; type=""+type+""; provider=""+provider);
   Marshal.ThrowExceptionForHR(Method<GetPattern>(element,16)(element,10000,out pattern));
   if(pattern==IntPtr.Zero) throw new InvalidOperationException(""InvokePattern unavailable"");
   var invokeId=new Guid(""fb377fbe-8ea6-46d5-9c73-6499642d3059"");
   Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pattern, ref invokeId, out invoke));
   Marshal.ThrowExceptionForHR(Method<Invoke>(invoke,3)(invoke));
  } finally { if(invoke!=IntPtr.Zero) Marshal.Release(invoke); if(pattern!=IntPtr.Zero) Marshal.Release(pattern); if(element!=IntPtr.Zero) Marshal.Release(element); if(automation!=IntPtr.Zero) Marshal.Release(automation); }
 }
}";
        return "$ErrorActionPreference='Stop'; Add-Type -TypeDefinition ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" +
            Convert.ToBase64String(Encoding.UTF8.GetBytes(source)) + "'))); [NativeUiaInvoke]::Run(" + handle.ToInt64() + ")";
    }
}
