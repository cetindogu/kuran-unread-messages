using KuranApp.Data;

namespace KuranApp.Data
{
    public static class DataInitializer
    {
        public static void Seed(DatabaseHelper db)
        {
            if (db.GetUserId("default_user") == -1)
            {
                db.AddUser("default_user");
            }

            if (db.GetUserId("test") == -1)
            {
                db.AddUser("test");
            }
        }
    }
}
