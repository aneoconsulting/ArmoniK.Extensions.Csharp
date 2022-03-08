using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArmoniK.DevelopmentKit.Common
{
  public class SerializeDelegate
  {

    [Serializable()]
    public sealed class MethodConstructor
        : Object
    {
      private readonly Type[] _parameterTypes;

      private readonly Type _returnType;

      private readonly Int32 _maxStackSize;

      private readonly Byte[] _methodBody;

      private readonly Byte[] _localSignature;

      public MethodConstructor(MethodInfo method)
      {
        this._parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        this._returnType = method.ReturnType;
        this._maxStackSize = method.GetMethodBody().MaxStackSize;
        this._methodBody = method.GetMethodBody().GetILAsByteArray();
        SignatureHelper helper = SignatureHelper.GetLocalVarSigHelper();
        helper.AddArguments(method.GetParameters().Select(p => p.ParameterType).ToArray(), null, null);
        this._localSignature = helper.GetSignature();
      }

      public static TDelegate Construct<TDelegate>(MethodConstructor data)
          where TDelegate : class
      {
        DynamicMethod method = new DynamicMethod(
            "<MethodConstructor>" + Guid.NewGuid().ToString("n"),
            data._returnType,
            data._parameterTypes
        );
        method.GetDynamicILInfo().SetCode(data._methodBody, data._maxStackSize);
        method.GetDynamicILInfo().SetLocalSignature(data._localSignature);
        return method.CreateDelegate(typeof(TDelegate)) as TDelegate;
      }

      public static Byte[] Serialize(Delegate d)
      {
        return Serialize(d.Method);
      }

      public static Byte[] Serialize(MethodInfo method)
      {
        using (MemoryStream stream = new MemoryStream())
        {
          new BinaryFormatter().Serialize(stream, new MethodConstructor(method));
          stream.Seek(0, SeekOrigin.Begin);
          return stream.ToArray();
        }
      }

      public static TDelegate Deserialize<TDelegate>(Byte[] data)
          where TDelegate : class
      {
        using (MemoryStream stream = new MemoryStream(data))
        {
          return Construct<TDelegate>((MethodConstructor)new BinaryFormatter().Deserialize(stream));
        }
      }
    }
  }
}
