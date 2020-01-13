using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Mono.Cecil.Beebyte;
using Mono.Cecil.Beebyte.Cil;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public class DetectJsonAOTCode
{
    private static bool hasGen = false;
    [PostProcessBuild(1000)]
#pragma warning disable IDE0051 // 删除未使用的私有成员
    private static void OnPostprocessBuildPlayer(BuildTarget buildTarget, string buildPath)
#pragma warning restore IDE0051 // 删除未使用的私有成员
    {
        hasGen = false;
    }

    [PostProcessScene]
    public static void TestInjectMothodOnPost()
    {
        if (hasGen == true) return;
        hasGen = true;

        TestInjectMothod();
    }

    [InitializeOnLoadMethod]
    public static void TestInjectMothod()
    {
        //  注：编辑器下注入后，可能会无法调试
        return;
        var path = Path.Combine(Application.dataPath, @"..\Library\ScriptAssemblies\Assembly-CSharp.dll");
        var assembly = AssemblyDefinition.ReadAssembly(path);
        var types = assembly.MainModule.GetTypes();
        foreach (var type in types) {
            foreach (var Method in type.Methods) {
                if (Method.Name == "InjectMod") {
                    InjectMethod(Method, assembly);
                    foreach (var item in Method.Body.Instructions) {
                        var str = item.ToString();
                        var reg = @"Newtonsoft.Json.JsonConvert::DeserializeObject<(.*)>";
                        var match = Regex.Match(str, reg);
                        if (match.Success) {
                            Debug.Log(match);
                        }
                    }
                }
            }
        }
        var writerParameters = new WriterParameters { WriteSymbols = true };
        assembly.Write(path, new WriterParameters());
    }

    private static void InjectMethod(MethodDefinition method, AssemblyDefinition assembly)
    {
        var firstIns = method.Body.Instructions.First();

        var worker = method.Body.GetILProcessor();

        //获取Debug.Log方法引用
        var hasPatchRef = assembly.MainModule.ImportReference(
        typeof(Debug).GetMethod("Log", new Type[] { typeof(string) }));
        //插入函数
        var current = InsertBefore(worker, firstIns, worker.Create(OpCodes.Ldstr, "Inject"));
        current = InsertBefore(worker, firstIns, worker.Create(OpCodes.Call, hasPatchRef));
        //计算Offset
        ComputeOffsets(method.Body);
    }

    /// <summary>
    /// 语句前插入Instruction, 并返回当前语句
    /// </summary>
    private static Instruction InsertBefore(ILProcessor worker, Instruction target, Instruction instruction)
    {
        worker.InsertBefore(target, instruction);
        return instruction;
    }

    /// <summary>
    /// 语句后插入Instruction, 并返回当前语句
    /// </summary>
    private static Instruction InsertAfter(ILProcessor worker, Instruction target, Instruction instruction)
    {
        worker.InsertAfter(target, instruction);
        return instruction;
    }
    //计算注入后的函数偏移值
    private static void ComputeOffsets(MethodBody body)
    {
        var offset = 0;
        foreach (var instruction in body.Instructions) {
            instruction.Offset = offset;
            offset += instruction.GetSize();
        }
    }

}
