using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet.Emit;
using dnlib.DotNet;

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

        public static void obfuscate_classes(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                string encName = random_string(10);
                Console.WriteLine($"{type.Name} -> {encName}");
                type.Name = encName;
            }

        }

        public static void obfuscate_namespace(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                string encName = random_string(10);
                Console.WriteLine($"{type.Namespace} -> {encName}");
                type.Namespace = encName;
            }

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
            //obfuscate_methods(md);
            obfuscate_classes(md);
            obfuscate_namespace(md);
            obfuscate_assembly_info(md);

            clean_asm(md);

            return md;
        }
    }
}
