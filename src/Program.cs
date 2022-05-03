class Program {
    public static int Main(string[] args) {
        var rootPath = args.Length == 1 ? args[0] : Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
        new LangPatcher(rootPath).Run();
        return 0;
    }
}
