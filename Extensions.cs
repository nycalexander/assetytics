public static class Extensions
{
    public static void RunSTA(this Thread thread)
    {
        thread.SetApartmentState(ApartmentState.STA); // Configure for STA
        thread.Start(); // Start running STA thread for action
    }
}