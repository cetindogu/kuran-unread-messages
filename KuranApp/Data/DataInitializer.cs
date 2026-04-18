using KuranApp.Data;

namespace KuranApp.Data
{
    public static class DataInitializer
    {
        public static void Seed(DatabaseHelper db)
        {
            // Veritabanı boşsa varsayılan kullanıcıyı oluştur
            if (db.GetUserId("default_user") == -1)
            {
                db.AddUser("default_user");
            }
        }
    }
}
