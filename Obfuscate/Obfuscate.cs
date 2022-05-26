using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Obfuscate
{
    internal class Obfuscate
    {
        private static Random random = new Random();
        private static List<String> names = new List<string>();

        public static string random_string(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string name = "";
            do
            {
                name = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            } while (names.Contains(name));

            return name;
        }

        public static void clean_asm(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;

                    method.Body.SimplifyBranches();
                    method.Body.OptimizeBranches();
                }
            }
        }

        public static void obfuscate_strings(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    for (int i = 0; i < method.Body.Instructions.Count(); i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            String regString = method.Body.Instructions[i].Operand.ToString();
                            String encString = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(regString));
                            Console.WriteLine($"{regString} -> {encString}");
                            method.Body.Instructions[i].OpCode = OpCodes.Nop;
                            method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, md.Import(typeof(System.Text.Encoding).GetMethod("get_UTF8", new Type[] { }))));
                            method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Ldstr, encString)); // Load string onto stack
                            method.Body.Instructions.Insert(i + 3, new Instruction(OpCodes.Call, md.Import(typeof(System.Convert).GetMethod("FromBase64String", new Type[] { typeof(string) }))));
                            method.Body.Instructions.Insert(i + 4, new Instruction(OpCodes.Callvirt, md.Import(typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));
                            i += 4;
                        }
                    }
                }
            }

        }


        public static void RandomOutlinedMethods(ModuleDef module)
        {
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods.ToArray())
                {
                    MethodDef strings = CreateReturnMethodDef(RandomString(), method);
                    MethodDef ints = CreateReturnMethodDef(RandomInt(), method);
                    type.Methods.Add(strings);
                    type.Methods.Add(ints);
                }
            }
        }
        public static string RandomString()
        {
            const string chars = "ABCD1234";
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[new Random(Guid.NewGuid().GetHashCode()).Next(s.Length)]).ToArray());
        }

        public static int RandomInt()
        {
            var ints = Convert.ToInt32(Regex.Match(Guid.NewGuid().ToString(), @"\d+").Value);
            return new Random(ints).Next(0, 99999999);
        }
        private static MethodDef CreateReturnMethodDef(object value, MethodDef source_method)
        {
            CorLibTypeSig corlib = null;

            if (value is int)
                corlib = source_method.Module.CorLibTypes.Int32;
            else if (value is float)
                corlib = source_method.Module.CorLibTypes.Single;
            else if (value is string)
                corlib = source_method.Module.CorLibTypes.String;
            MethodDef newMethod = new MethodDefUser(RandomString(),
                    MethodSig.CreateStatic(corlib),
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            {
                Body = new CilBody()
            };
            if (value is int)
                newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)value));
            else if (value is float)
                newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, (double)value));
            else if (value is string)
                newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, (string)value));
            newMethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));
            return newMethod;
        }

        public static void obfuscate_assembly_info(ModuleDef md)
        {
            string encName = random_string(10);
            Console.WriteLine($"{md.Assembly.Name} -> {encName}");
            md.Assembly.Name = encName;

            string[] attri = { "AssemblyDescriptionAttribute", "AssemblyTitleAttribute", "AssemblyProductAttribute", "AssemblyCopyrightAttribute", "AssemblyCompanyAttribute", "AssemblyFileVersionAttribute" };
            foreach (CustomAttribute attribute in md.Assembly.CustomAttributes)
            {
                if (attri.Any(attribute.AttributeType.Name.Contains))
                {
                    string encAttri = random_string(10);
                    Console.WriteLine($"{attribute.AttributeType.Name} = {encAttri}");
                    attribute.ConstructorArguments[0] = new CAArgument(md.CorLibTypes.String, new UTF8String(encAttri));
                }
            }
        }

        public static ModuleDefMD obfuscate(ModuleDefMD md)
        {
            md.Name = random_string(10);

            obfuscate_strings(md);
            RandomOutlinedMethods(md);
            //obfuscate_assembly_info(md);

            clean_asm(md);

            return md;
        }
    }
}
