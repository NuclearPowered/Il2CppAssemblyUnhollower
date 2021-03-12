using AssemblyUnhollower.Contexts;

namespace AssemblyUnhollower.Passes
{
    public static class Pass18FinalizeMethodContexts
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
            foreach (var methodContext in typeContext.Methods)
            {
                methodContext.CtorPhase2();
            }
        }
    }
}
