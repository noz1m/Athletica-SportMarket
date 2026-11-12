using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, string? adminEmail = null, string? adminPassword = null)
    {
        await db.Database.MigrateAsync();

        // Ensure Description column exists on Banners table (migration fallback)
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name='Banners' AND column_name='Description'
                ) THEN
                    ALTER TABLE "Banners" ADD COLUMN "Description" text NULL;
                END IF;
            END $$;
            """
        );

        // ── Permissions ───────────────────────────────────────────────────
        var allPermNames = Permissions.ContentManagerPermissions
            .Concat(Permissions.SuperAdminExtra)
            .Distinct()
            .ToList();

        foreach (var name in allPermNames)
        {
            if (!await db.Permissions.AnyAsync(p => p.Name == name))
                db.Permissions.Add(new Permission { Name = name });
        }
        await db.SaveChangesAsync();

        var permDict = await db.Permissions.ToDictionaryAsync(p => p.Name);

        // ── Roles ─────────────────────────────────────────────────────────
        async Task<Role> EnsureRole(string roleName, string[] permNames)
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role == null)
            {
                role = new Role { Name = roleName };
                db.Roles.Add(role);
                await db.SaveChangesAsync();
            }

            foreach (var pn in permNames)
            {
                if (!role.RolePermissions.Any(rp => rp.PermissionId == permDict[pn].Id))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId       = role.Id,
                        PermissionId = permDict[pn].Id
                    });
                }
            }

            await db.SaveChangesAsync();
            return role;
        }

        var buyerRole = await EnsureRole("Buyer", []);
        var managerRole = await EnsureRole("ContentManager", Permissions.ContentManagerPermissions);
        var superAdminRole = await EnsureRole("SuperAdmin",
            Permissions.ContentManagerPermissions.Concat(Permissions.SuperAdminExtra).ToArray());

        // ── SuperAdmin user ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            !string.IsNullOrWhiteSpace(adminPassword) &&
            adminPassword.Length >= 8 &&
            !await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new User
            {
                Email        = adminEmail!,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword!),
                FirstName    = "Super",
                LastName     = "Admin",
                RoleId       = superAdminRole.Id
            });
        }

        // ── Default site settings ─────────────────────────────────────────
        var defaultSettings = new Dictionary<string, string>
        {
            ["phone"]            = "+7 (999) 000-00-00",
            ["email"]            = "hello@athletica.example",
            ["vk_url"]           = "",
            ["instagram_url"]    = "",
            ["telegram_url"]     = "",
            ["site_description"] = "Экипировка, одежда и аксессуары для спорта и активной жизни.",
            ["privacy_policy"]   = "Политика конфиденциальности",
            ["terms_of_service"] = "Пользовательское соглашение",
            // Site branding
            ["site_name"]        = "Athletica",
            ["site_logo_url"]    = "",
            // Trust block on home page
            ["trust_title"]      = "Почему выбирают нас",
            ["trust_1_title"]    = "Для любого уровня",
            ["trust_1_desc"]     = "Товары для первых тренировок и новых спортивных целей",
            ["trust_2_title"]    = "Выбор для движения",
            ["trust_2_desc"]     = "Подбирайте одежду, обувь и спортивные аксессуары в одном каталоге",
            ["trust_3_title"]    = "Удобный выбор",
            ["trust_3_desc"]     = "Удобный каталог, фильтры и избранное для ваших покупок",
            // Marketplace links
            ["ozon_url"]         = "",
            ["wb_url"]           = "",
            // Articles page texts
            ["articles_title"]    = "Журнал Athletica",
            ["articles_subtitle"] = "Тренировки, экипировка и идеи для активной жизни",
            // Gallery images (JSON arrays of URLs)
            ["cert_images"]             = "[]",
            ["about_person_images"]     = "[]",
            ["about_company_images"]    = "[]",
        };

        // Rename only the original starter branding, never overwrite a customized shop.
        var legacyBrand = await db.SiteSettings.FirstOrDefaultAsync(x => x.Key == "site_name");
        if (legacyBrand?.Value == "KamilKarate") legacyBrand.Value = "Athletica";
        var legacyDescription = await db.SiteSettings.FirstOrDefaultAsync(x => x.Key == "site_description");
        if (legacyDescription?.Value == "Натуральные БАДы и витамины высшего качества для вашего здоровья.")
            legacyDescription.Value = "Экипировка, одежда и аксессуары для спорта и активной жизни.";

        var legacyDefaults = new Dictionary<string, string>
        {
            ["trust_1_title"] = "Сертифицировано",
            ["trust_1_desc"] = "Все продукты прошли государственную сертификацию и имеют документы качества",
            ["trust_2_title"] = "Натуральный состав",
            ["trust_2_desc"] = "Используем только натуральные компоненты без искусственных добавок",
            ["trust_3_title"] = "Разработано экспертами",
            ["trust_3_desc"] = "Формулы созданы нутрициологами с многолетним опытом",
            ["articles_title"] = "Блог о здоровье",
            ["articles_subtitle"] = "Экспертные статьи о витаминах, здоровье и правильном питании"
        };
        foreach (var (key, oldValue) in legacyDefaults)
        {
            var current = await db.SiteSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (current?.Value == oldValue) current.Value = defaultSettings[key];
        }

        foreach (var (key, val) in defaultSettings)
        {
            if (!await db.SiteSettings.AnyAsync(s => s.Key == key))
                db.SiteSettings.Add(new SiteSetting { Key = key, Value = val });
        }

        // ── Default page content blocks ───────────────────────────────────
        var defaultPages = new[]
        {
            ("about_person",  "О главном лице"),
            ("about_company", "О компании"),
            ("certificates",  "Сертификаты"),
        };

        foreach (var (key, title) in defaultPages)
        {
            if (!await db.PageContents.AnyAsync(p => p.Key == key))
                db.PageContents.Add(new PageContent { Key = key, Title = title, Content = "" });
        }

        await db.SaveChangesAsync();
    }
}
