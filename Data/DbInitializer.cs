using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Data
{
    // Creates the database, seeds RBAC roles + a default account per role,
    // and loads sample pharmacy data so the system is demo-ready on first run.
    public static class DbInitializer
    {
        // The five actors from the Use Case Diagram
        public static readonly string[] Roles =
        {
            "Admin",                // Main Admin
            "Pharmacist",           // Pharmacist
            "Cashier",              // Cashier / Sales Staff
            "InventoryCoordinator", // Inventory Coordinator
            "Customer"              // Customer
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // ---- 1. Roles (RBAC) ----
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---- 2. One demo user per role ----
            await CreateUserIfNotExists(userManager, "admin@careplus.ph", "Admin@123", "Main Admin", "Admin");
            await CreateUserIfNotExists(userManager, "pharmacist@careplus.ph", "Pharmacist@123", "Pharmacist User", "Pharmacist");
            await CreateUserIfNotExists(userManager, "cashier@careplus.ph", "Cashier@123", "Cashier User", "Cashier");
            await CreateUserIfNotExists(userManager, "inventory@careplus.ph", "Inventory@123", "Inventory Coordinator", "InventoryCoordinator");
            await CreateUserIfNotExists(userManager, "customer@careplus.ph", "Customer@123", "Customer User", "Customer");

            // ---- 3. Sample domain data (only if empty) ----
            if (!context.Suppliers.Any())
            {
                var suppliers = new List<Supplier>
                {
                    new() { Name = "MediSource PH", ContactNumber = "0917-111-2222", Email = "sales@medisource.ph", Address = "Davao City, Philippines" },
                    new() { Name = "PharmaLink Distributors", ContactNumber = "0917-333-4444", Email = "orders@pharmalink.com", Address = "Cebu City, Philippines" },
                    new() { Name = "HealthWell Supply Co.", ContactNumber = "0917-555-6666", Email = "info@healthwell.ph", Address = "Metro Manila, Philippines" }
                };
                context.Suppliers.AddRange(suppliers);
                await context.SaveChangesAsync();

                var medicines = new List<Medicine>
                {
                    new() { Name = "Biogesic 500mg", GenericName = "Paracetamol", Category = "Analgesic / Antipyretic", Manufacturer = "Unilab", UnitPrice = 8.50m, ReorderLevel = 50, SupplierId = suppliers[0].Id, Description = "Fast relief for headache, minor aches, and fever." },
                    new() { Name = "Amoxil 500mg", GenericName = "Amoxicillin Trihydrate", Category = "Antibiotics", Manufacturer = "GSK (GlaxoSmithKline)", UnitPrice = 16.00m, ReorderLevel = 40, SupplierId = suppliers[1].Id, Description = "Broad-spectrum prescription antibiotic for bacterial infections." },
                    new() { Name = "Advil 200mg Softgel", GenericName = "Ibuprofen Liqui-Gels", Category = "NSAID / Pain Relief", Manufacturer = "Haleon / Pfizer", UnitPrice = 12.75m, ReorderLevel = 50, SupplierId = suppliers[0].Id, Description = "Targeted relief for acute muscular and inflammatory pain." },
                    new() { Name = "Zyrtec 10mg", GenericName = "Cetirizine Dihydrochloride", Category = "Antihistamine / Allergy", Manufacturer = "Johnson & Johnson", UnitPrice = 28.50m, ReorderLevel = 30, SupplierId = suppliers[2].Id, Description = "24-hour relief from allergy, rhinitis, and urticaria." },
                    new() { Name = "Cozaar 50mg", GenericName = "Losartan Potassium", Category = "Cardiovascular / Antihypertensive", Manufacturer = "Organon / MSD", UnitPrice = 22.00m, ReorderLevel = 30, SupplierId = suppliers[1].Id, Description = "Essential daily maintenance for blood pressure and kidney protection." },
                    new() { Name = "Glucophage 500mg", GenericName = "Metformin Hydrochloride", Category = "Diabetes Care", Manufacturer = "Merck", UnitPrice = 14.50m, ReorderLevel = 40, SupplierId = suppliers[2].Id, Description = "First-line oral antidiabetic medication for Type 2 diabetes." },
                    new() { Name = "Solmux Advance 500mg", GenericName = "Carbocisteine + Zinc", Category = "Respiratory / Cough", Manufacturer = "Unilab", UnitPrice = 15.00m, ReorderLevel = 35, SupplierId = suppliers[0].Id, Description = "Dual action mucolytic for productive cough with phlegm." },
                    new() { Name = "Kremil-S Advance", GenericName = "Famotidine + Calcium Carb + Mag Hydroxide", Category = "Antacid / Gastrointestinal", Manufacturer = "Unilab", UnitPrice = 19.50m, ReorderLevel = 25, SupplierId = suppliers[0].Id, Description = "Fast relief from heartburn, hyperacidity, and acid reflux." }
                };
                context.Medicines.AddRange(medicines);
                await context.SaveChangesAsync();

                context.MedicineBatches.AddRange(
                    new MedicineBatch { MedicineId = medicines[0].Id, BatchNumber = "B-2026-01", Quantity = 250, ExpiryDate = DateTime.Today.AddMonths(14), DateReceived = DateTime.Today.AddDays(-30) },
                    new MedicineBatch { MedicineId = medicines[0].Id, BatchNumber = "B-2026-02", Quantity = 80, ExpiryDate = DateTime.Today.AddMonths(8), DateReceived = DateTime.Today.AddDays(-60) },
                    new MedicineBatch { MedicineId = medicines[1].Id, BatchNumber = "B-2026-03", Quantity = 35, ExpiryDate = DateTime.Today.AddDays(25), DateReceived = DateTime.Today.AddDays(-120) }, // Critical near-expiry
                    new MedicineBatch { MedicineId = medicines[2].Id, BatchNumber = "B-2026-04", Quantity = 14, ExpiryDate = DateTime.Today.AddDays(70), DateReceived = DateTime.Today.AddDays(-90) }, // Warning near-expiry + low stock
                    new MedicineBatch { MedicineId = medicines[3].Id, BatchNumber = "B-2026-05", Quantity = 120, ExpiryDate = DateTime.Today.AddMonths(18), DateReceived = DateTime.Today.AddDays(-15) },
                    new MedicineBatch { MedicineId = medicines[4].Id, BatchNumber = "B-2026-06", Quantity = 95, ExpiryDate = DateTime.Today.AddMonths(11), DateReceived = DateTime.Today.AddDays(-45) },
                    new MedicineBatch { MedicineId = medicines[5].Id, BatchNumber = "B-2026-07", Quantity = 110, ExpiryDate = DateTime.Today.AddMonths(15), DateReceived = DateTime.Today.AddDays(-10) },
                    new MedicineBatch { MedicineId = medicines[6].Id, BatchNumber = "B-2026-08", Quantity = 160, ExpiryDate = DateTime.Today.AddMonths(9), DateReceived = DateTime.Today.AddDays(-20) },
                    new MedicineBatch { MedicineId = medicines[7].Id, BatchNumber = "B-2026-09", Quantity = 18, ExpiryDate = DateTime.Today.AddDays(-5), DateReceived = DateTime.Today.AddDays(-180) } // Expired batch demo
                );
                await context.SaveChangesAsync();

                var customers = new List<Customer>
                {
                    new() { FullName = "Juan Dela Cruz", Phone = "0918-222-1111", Email = "juan.delacruz@mail.com", Address = "Agdao, Davao City", LoyaltyPoints = 320, DateRegistered = DateTime.Today.AddMonths(-6) },
                    new() { FullName = "Maria Santos", Phone = "0918-333-2222", Email = "maria.santos@mail.com", Address = "Buhangin, Davao City", LoyaltyPoints = 145, DateRegistered = DateTime.Today.AddMonths(-4) },
                    new() { FullName = "Ricardo Tan", Phone = "0919-444-5555", Email = "ricardo.tan@mail.com", Address = "Matina, Davao City", LoyaltyPoints = 580, DateRegistered = DateTime.Today.AddYears(-1) },
                    new() { FullName = "Elena Villanueva", Phone = "0920-666-7777", Email = "elena.v@mail.com", Address = "Toril, Davao City", LoyaltyPoints = 65, DateRegistered = DateTime.Today.AddMonths(-1) }
                };
                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();

                // Seed sample prescription records
                var rx1 = new Prescription
                {
                    CustomerId = customers[0].Id,
                    DoctorName = "Dr. Rafael Mendoza, MD (Cardiology)",
                    DatePrescribed = DateTime.Today.AddDays(-10),
                    Status = PrescriptionStatus.Fulfilled,
                    Details = new List<PrescriptionDetail>
                    {
                        new() { MedicineId = medicines[4].Id, Quantity = 30, Dosage = "Take 1 tablet (50mg) once daily every morning." }
                    }
                };
                var rx2 = new Prescription
                {
                    CustomerId = customers[1].Id,
                    DoctorName = "Dr. Corazon Aquino-Reyes, MD (Internal Medicine)",
                    DatePrescribed = DateTime.Today.AddDays(-3),
                    Status = PrescriptionStatus.Pending,
                    Details = new List<PrescriptionDetail>
                    {
                        new() { MedicineId = medicines[1].Id, Quantity = 21, Dosage = "Take 1 capsule 3x daily every 8 hours for 7 days." }
                    }
                };
                var rx3 = new Prescription
                {
                    CustomerId = customers[2].Id,
                    DoctorName = "Dr. Arturo Gomez, MD (Endocrinology)",
                    DatePrescribed = DateTime.Today.AddDays(-25),
                    Status = PrescriptionStatus.Fulfilled,
                    Details = new List<PrescriptionDetail>
                    {
                        new() { MedicineId = medicines[5].Id, Quantity = 60, Dosage = "Take 1 tablet (500mg) twice daily with meals." }
                    }
                };
                context.Prescriptions.AddRange(rx1, rx2, rx3);
                await context.SaveChangesAsync();

                // Seed sample purchase orders
                var po1 = new PurchaseOrder
                {
                    SupplierId = suppliers[0].Id,
                    OrderDate = DateTime.Today.AddDays(-20),
                    Status = PurchaseOrderStatus.Received,
                    Details = new List<PurchaseOrderDetail>
                    {
                        new() { MedicineId = medicines[0].Id, Quantity = 200, UnitCost = 6.00m },
                        new() { MedicineId = medicines[6].Id, Quantity = 100, UnitCost = 11.50m }
                    }
                };
                var po2 = new PurchaseOrder
                {
                    SupplierId = suppliers[1].Id,
                    OrderDate = DateTime.Today.AddDays(-5),
                    Status = PurchaseOrderStatus.Pending,
                    Details = new List<PurchaseOrderDetail>
                    {
                        new() { MedicineId = medicines[4].Id, Quantity = 150, UnitCost = 16.50m }
                    }
                };
                context.PurchaseOrders.AddRange(po1, po2);
                await context.SaveChangesAsync();

                // Seed sample audit logs
                context.AuditLogs.AddRange(
                    new AuditLog { Timestamp = DateTime.Now.AddHours(-12), UserName = "Main Admin", UserRole = "Admin", Action = "SYSTEM_INITIALIZED", Module = "System", Details = "CarePlus Pharmacy ERP & CRM system initialized with RBAC security." },
                    new AuditLog { Timestamp = DateTime.Now.AddHours(-6), UserName = "Inventory Coordinator", UserRole = "InventoryCoordinator", Action = "BATCH_RECEIVED", Module = "Inventory", Details = "Received stock batch B-2026-01 (Biogesic 500mg, Qty: 250)." },
                    new AuditLog { Timestamp = DateTime.Now.AddHours(-3), UserName = "Pharmacist User", UserRole = "Pharmacist", Action = "PRESCRIPTION_VERIFIED", Module = "Prescriptions", Details = "Verified prescription by Dr. Rafael Mendoza for patient Juan Dela Cruz." },
                    new AuditLog { Timestamp = DateTime.Now.AddHours(-1), UserName = "Cashier User", UserRole = "Cashier", Action = "SALE_COMPLETED", Module = "POS", Details = "Completed POS sale with cash payment and issued invoice INV-00001." }
                );
                await context.SaveChangesAsync();
            }
            else
            {
                // Ensure existing medicines have generic names & manufacturers populated
                var medsToUpdate = await context.Medicines.Where(m => string.IsNullOrEmpty(m.GenericName)).ToListAsync();
                if (medsToUpdate.Any())
                {
                    foreach (var m in medsToUpdate)
                    {
                        if (m.Name.Contains("Paracetamol")) { m.GenericName = "Paracetamol"; m.Manufacturer = "Unilab"; }
                        else if (m.Name.Contains("Amoxicillin")) { m.GenericName = "Amoxicillin Trihydrate"; m.Manufacturer = "GSK"; }
                        else if (m.Name.Contains("Ibuprofen")) { m.GenericName = "Ibuprofen"; m.Manufacturer = "Pfizer"; }
                        else if (m.Name.Contains("Cetirizine")) { m.GenericName = "Cetirizine Dihydrochloride"; m.Manufacturer = "Johnson & Johnson"; }
                        else if (m.Name.Contains("Losartan")) { m.GenericName = "Losartan Potassium"; m.Manufacturer = "MSD"; }
                        else if (m.Name.Contains("Metformin")) { m.GenericName = "Metformin Hydrochloride"; m.Manufacturer = "Merck"; }
                        else { m.GenericName = m.Name; m.Manufacturer = "Standard Pharma"; }
                    }
                    await context.SaveChangesAsync();
                }

                // Ensure customers have loyalty points populated
                var custsToUpdate = await context.Customers.Where(c => c.LoyaltyPoints == 0).ToListAsync();
                if (custsToUpdate.Any())
                {
                    int pts = 120;
                    foreach (var c in custsToUpdate)
                    {
                        c.LoyaltyPoints = pts;
                        pts += 150;
                    }
                    await context.SaveChangesAsync();
                }

                // Seed audit logs if empty
                if (!context.AuditLogs.Any())
                {
                    context.AuditLogs.AddRange(
                        new AuditLog { Timestamp = DateTime.Now.AddHours(-12), UserName = "Main Admin", UserRole = "Admin", Action = "SYSTEM_UPDATE", Module = "System", Details = "CarePlus Pharmacy upgraded to full ERP and CRM suite." },
                        new AuditLog { Timestamp = DateTime.Now.AddHours(-5), UserName = "Pharmacist User", UserRole = "Pharmacist", Action = "POLICY_AUDIT", Module = "Compliance", Details = "Completed scheduled expiry and temperature storage audit." }
                    );
                    await context.SaveChangesAsync();
                }
            }

            // ---- 4. Ensure Rich 50+ Dataset for Billing, Medicines, Sales, and Customers ----
            await EnsureRichDataSeededAsync(context, userManager);
        }

        private static async Task CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            string email, string password, string fullName, string role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
        private static async Task EnsureRichDataSeededAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Ensure Suppliers (at least 5 suppliers)
            if (await context.Suppliers.CountAsync() < 5)
            {
                var extraSuppliers = new List<Supplier>
                {
                    new() { Name = "Zuellig Pharma Philippines", ContactNumber = "0917-777-8888", Email = "orders@zuelligpharma.com.ph", Address = "Km. 14 West Service Rd, Taguig, Metro Manila" },
                    new() { Name = "United Laboratories Logistics", ContactNumber = "0917-888-9999", Email = "supply@unilab.com.ph", Address = "66 United St, Mandaluyong, Metro Manila" },
                    new() { Name = "Pascual Laboratories", ContactNumber = "0917-999-0000", Email = "contact@pascuallab.com", Address = "Balagtas, Bulacan, Philippines" }
                };
                context.Suppliers.AddRange(extraSuppliers);
                await context.SaveChangesAsync();
            }

            var suppliers = await context.Suppliers.ToListAsync();
            var supplierId1 = suppliers[0].Id;
            var supplierId2 = suppliers.Count > 1 ? suppliers[1].Id : supplierId1;
            var supplierId3 = suppliers.Count > 2 ? suppliers[2].Id : supplierId1;

            // 2. Ensure Medicines (at least 35 medicines)
            if (await context.Medicines.CountAsync() < 35)
            {
                var extraMeds = new List<Medicine>
                {
                    new() { Name = "Tempra 500mg", GenericName = "Paracetamol", Category = "Analgesic / Antipyretic", Manufacturer = "Taisho Pharma", UnitPrice = 7.50m, ReorderLevel = 50, SupplierId = supplierId1, Description = "Trusted pain reliever and fever reducer." },
                    new() { Name = "Calpol 250mg Suspension", GenericName = "Paracetamol Pediatric", Category = "Analgesic / Antipyretic", Manufacturer = "GSK", UnitPrice = 145.00m, ReorderLevel = 25, SupplierId = supplierId2, Description = "Fast fever relief for children 6 to 12 years." },
                    new() { Name = "Alaxan FR", GenericName = "Paracetamol + Ibuprofen", Category = "Analgesic / Muscle Pain", Manufacturer = "Unilab", UnitPrice = 11.50m, ReorderLevel = 60, SupplierId = supplierId1, Description = "Fast relief for body aches, muscle pain, and headache." },
                    new() { Name = "Dolfenal 500mg", GenericName = "Mefenamic Acid", Category = "NSAID / Pain Relief", Manufacturer = "Unilab", UnitPrice = 18.00m, ReorderLevel = 40, SupplierId = supplierId1, Description = "Relief of mild to moderate pain including toothache and dysmenorrhea." },
                    new() { Name = "Celebrex 200mg", GenericName = "Celecoxib", Category = "NSAID / Anti-inflammatory", Manufacturer = "Pfizer", UnitPrice = 48.50m, ReorderLevel = 30, SupplierId = supplierId3, Description = "Cox-2 inhibitor for arthritis and acute pain management." },
                    new() { Name = "Arcoxia 90mg", GenericName = "Etoricoxib", Category = "NSAID / Osteoarthritis", Manufacturer = "Organon", UnitPrice = 72.00m, ReorderLevel = 25, SupplierId = supplierId3, Description = "Selective inhibitor for osteoarthritis and rheumatoid pain." },
                    new() { Name = "Augmentin 625mg", GenericName = "Amoxicillin + Clavulanic Acid", Category = "Antibiotics", Manufacturer = "GSK", UnitPrice = 45.00m, ReorderLevel = 35, SupplierId = supplierId2, Description = "Broad-spectrum co-amoxiclav for resistant respiratory infections." },
                    new() { Name = "Keflex 500mg", GenericName = "Cephalexin Monohydrate", Category = "Antibiotics", Manufacturer = "Euro-Med", UnitPrice = 26.50m, ReorderLevel = 30, SupplierId = supplierId2, Description = "First generation cephalosporin antibiotic for skin and soft tissue." },
                    new() { Name = "Zithromax 500mg", GenericName = "Azithromycin", Category = "Antibiotics", Manufacturer = "Pfizer", UnitPrice = 115.00m, ReorderLevel = 20, SupplierId = supplierId3, Description = "Macrolide antibiotic 3-day short course therapy." },
                    new() { Name = "Ciprox 500mg", GenericName = "Ciprofloxacin", Category = "Antibiotics", Manufacturer = "Pascual Lab", UnitPrice = 32.00m, ReorderLevel = 30, SupplierId = supplierId1, Description = "Fluoroquinolone for urinary and gastrointestinal infections." },
                    new() { Name = "Claritin 10mg", GenericName = "Loratadine", Category = "Antihistamine / Allergy", Manufacturer = "Bayer", UnitPrice = 36.00m, ReorderLevel = 30, SupplierId = supplierId3, Description = "Non-drowsy 24-hour allergy symptom relief." },
                    new() { Name = "Allerkid 5mg/5mL", GenericName = "Cetirizine Hydrochloride", Category = "Antihistamine / Allergy", Manufacturer = "Unilab", UnitPrice = 165.00m, ReorderLevel = 20, SupplierId = supplierId1, Description = "Pediatric antihistamine oral solution." },
                    new() { Name = "Benadryl AH 25mg", GenericName = "Diphenhydramine HCl", Category = "Antihistamine / Allergy", Manufacturer = "Johnson & Johnson", UnitPrice = 14.00m, ReorderLevel = 35, SupplierId = supplierId2, Description = "Antihistamine for acute allergic reactions and motion sickness." },
                    new() { Name = "Norvasc 5mg", GenericName = "Amlodipine Besylate", Category = "Cardiovascular / Antihypertensive", Manufacturer = "Viatris", UnitPrice = 24.50m, ReorderLevel = 50, SupplierId = supplierId3, Description = "Calcium channel blocker for essential hypertension." },
                    new() { Name = "Micardis 40mg", GenericName = "Telmisartan", Category = "Cardiovascular / Antihypertensive", Manufacturer = "Boehringer Ingelheim", UnitPrice = 38.00m, ReorderLevel = 30, SupplierId = supplierId3, Description = "Angiotensin receptor blocker for 24-hour blood pressure control." },
                    new() { Name = "Betaloc 50mg", GenericName = "Metoprolol Tartrate", Category = "Cardiovascular / Antihypertensive", Manufacturer = "AstraZeneca", UnitPrice = 19.50m, ReorderLevel = 35, SupplierId = supplierId2, Description = "Cardioselective beta-blocker for hypertension and angina." },
                    new() { Name = "Catapres 75mcg", GenericName = "Clonidine HCl", Category = "Cardiovascular / Antihypertensive", Manufacturer = "Boehringer Ingelheim", UnitPrice = 28.00m, ReorderLevel = 25, SupplierId = supplierId3, Description = "Centrally acting antihypertensive for hypertensive crisis." },
                    new() { Name = "Januvia 100mg", GenericName = "Sitagliptin", Category = "Diabetes Care", Manufacturer = "MSD", UnitPrice = 65.00m, ReorderLevel = 25, SupplierId = supplierId3, Description = "DPP-4 inhibitor improving glycemic control in type 2 diabetes." },
                    new() { Name = "Diamicron MR 60mg", GenericName = "Gliclazide", Category = "Diabetes Care", Manufacturer = "Servier", UnitPrice = 21.00m, ReorderLevel = 35, SupplierId = supplierId2, Description = "Modified release sulfonylurea antidiabetic." },
                    new() { Name = "Forxiga 10mg", GenericName = "Dapagliflozin", Category = "Diabetes Care", Manufacturer = "AstraZeneca", UnitPrice = 58.00m, ReorderLevel = 20, SupplierId = supplierId2, Description = "SGLT2 inhibitor for glycemic control and renal protection." },
                    new() { Name = "Fluimucil 600mg", GenericName = "Acetylcysteine", Category = "Respiratory / Cough", Manufacturer = "Zambon", UnitPrice = 42.00m, ReorderLevel = 30, SupplierId = supplierId3, Description = "Effervescent mucolytic and powerful antioxidant." },
                    new() { Name = "Robitussin DM Syrup", GenericName = "Dextromethorphan + Guaifenesin", Category = "Respiratory / Cough", Manufacturer = "Haleon", UnitPrice = 135.00m, ReorderLevel = 25, SupplierId = supplierId1, Description = "Expectorant and cough suppressant formula." },
                    new() { Name = "Ventolin Inhaler", GenericName = "Salbutamol Sulfate 100mcg", Category = "Respiratory / Asthma", Manufacturer = "GSK", UnitPrice = 340.00m, ReorderLevel = 15, SupplierId = supplierId2, Description = "Metered dose inhaler for acute asthma relief." },
                    new() { Name = "Symbicort Turbuhaler", GenericName = "Budesonide + Formoterol", Category = "Respiratory / Asthma", Manufacturer = "AstraZeneca", UnitPrice = 1180.00m, ReorderLevel = 10, SupplierId = supplierId2, Description = "Dual maintenance and reliever therapy for asthma and COPD." },
                    new() { Name = "Gaviscon Double Action", GenericName = "Sodium Alginate + Antacid", Category = "Antacid / Gastrointestinal", Manufacturer = "Reckitt Benckiser", UnitPrice = 34.00m, ReorderLevel = 40, SupplierId = supplierId1, Description = "Fast soothing barrier against heartburn and indigestion." },
                    new() { Name = "Omepron 20mg", GenericName = "Omeprazole", Category = "Antacid / Gastrointestinal", Manufacturer = "Unilab", UnitPrice = 22.50m, ReorderLevel = 50, SupplierId = supplierId1, Description = "Proton pump inhibitor for hyperacidity, GERD, and ulcers." },
                    new() { Name = "Nexium 40mg", GenericName = "Esomeprazole Magnesium", Category = "Antacid / Gastrointestinal", Manufacturer = "AstraZeneca", UnitPrice = 85.00m, ReorderLevel = 20, SupplierId = supplierId2, Description = "Potent acid suppression for erosive esophagitis." },
                    new() { Name = "Buscopan 10mg", GenericName = "Hyoscine-N-Butylbromide", Category = "Antacid / Gastrointestinal", Manufacturer = "Sanofi", UnitPrice = 18.50m, ReorderLevel = 45, SupplierId = supplierId3, Description = "Targeted relief for abdominal spasms and cramps." },
                    new() { Name = "Buscopan Venus", GenericName = "Hyoscine + Paracetamol", Category = "Antacid / Gastrointestinal", Manufacturer = "Sanofi", UnitPrice = 26.00m, ReorderLevel = 35, SupplierId = supplierId3, Description = "Dual action for menstrual pains and pelvic cramps." },
                    new() { Name = "Enervon-C Tablet", GenericName = "Vitamin B-Complex + C", Category = "Vitamins & Supplements", Manufacturer = "Unilab", UnitPrice = 7.50m, ReorderLevel = 60, SupplierId = supplierId1, Description = "Daily energy and immune system booster." },
                    new() { Name = "Conzace Softgel", GenericName = "Zinc + Vitamins A, C, E", Category = "Vitamins & Supplements", Manufacturer = "Unilab", UnitPrice = 14.00m, ReorderLevel = 50, SupplierId = supplierId1, Description = "Immunity and healthy skin multivitamin supplement." },
                    new() { Name = "Centrum Advance", GenericName = "Multivitamins + Minerals", Category = "Vitamins & Supplements", Manufacturer = "Haleon", UnitPrice = 16.50m, ReorderLevel = 40, SupplierId = supplierId3, Description = "Complete micronutrient dietary supplement." },
                    new() { Name = "Fern-C 500mg", GenericName = "Sodium Ascorbate", Category = "Vitamins & Supplements", Manufacturer = "STADA", UnitPrice = 8.50m, ReorderLevel = 60, SupplierId = supplierId1, Description = "Non-acidic stomach-friendly vitamin C capsule." },
                    new() { Name = "Neurobion Forte", GenericName = "Vitamins B1, B6, B12", Category = "Vitamins & Supplements", Manufacturer = "P&G Health", UnitPrice = 28.00m, ReorderLevel = 35, SupplierId = supplierId2, Description = "Nerve nourishment and neuropathic symptom relief." },
                    new() { Name = "Lipitor 20mg", GenericName = "Atorvastatin Calcium", Category = "Cardiovascular / Statin", Manufacturer = "Viatris", UnitPrice = 39.50m, ReorderLevel = 40, SupplierId = supplierId3, Description = "Lowers LDL cholesterol and cardiovascular risk." },
                    new() { Name = "Crestor 10mg", GenericName = "Rosuvastatin Calcium", Category = "Cardiovascular / Statin", Manufacturer = "AstraZeneca", UnitPrice = 44.00m, ReorderLevel = 35, SupplierId = supplierId2, Description = "High-efficacy statin for hypercholesterolemia management." }
                };

                context.Medicines.AddRange(extraMeds);
                await context.SaveChangesAsync();

                // Add batches for each new medicine
                var batchList = new List<MedicineBatch>();
                int batchIndex = 10;
                foreach (var m in extraMeds)
                {
                    batchList.Add(new MedicineBatch
                    {
                        MedicineId = m.Id,
                        BatchNumber = $"B-2026-{batchIndex++:D2}",
                        Quantity = 120,
                        ExpiryDate = DateTime.Today.AddMonths(12 + (batchIndex % 14)),
                        DateReceived = DateTime.Today.AddDays(-20 - (batchIndex % 30))
                    });

                    if (batchIndex % 2 == 0)
                    {
                        batchList.Add(new MedicineBatch
                        {
                            MedicineId = m.Id,
                            BatchNumber = $"B-2026-{batchIndex++:D2}",
                            Quantity = 45,
                            ExpiryDate = DateTime.Today.AddMonths(6 + (batchIndex % 10)),
                            DateReceived = DateTime.Today.AddDays(-60)
                        });
                    }
                }
                context.MedicineBatches.AddRange(batchList);
                await context.SaveChangesAsync();
            }

            // 3. Ensure Customers (at least 30 customers)
            if (await context.Customers.CountAsync() < 30)
            {
                var extraCustomers = new List<Customer>
                {
                    new() { FullName = "Gabriel Dizon", Phone = "0917-101-2001", Email = "gabriel.dizon@mail.com", Address = "Lanang, Davao City", LoyaltyPoints = 420, DateRegistered = DateTime.Today.AddMonths(-8) },
                    new() { FullName = "Andrea Lim", Phone = "0917-102-2002", Email = "andrea.lim@mail.com", Address = "Bajada, Davao City", LoyaltyPoints = 260, DateRegistered = DateTime.Today.AddMonths(-7) },
                    new() { FullName = "Mark Anthony Castro", Phone = "0917-103-2003", Email = "m.castro@mail.com", Address = "Calinan, Davao City", LoyaltyPoints = 180, DateRegistered = DateTime.Today.AddMonths(-5) },
                    new() { FullName = "Rowena Bautista", Phone = "0917-104-2004", Email = "rowena.b@mail.com", Address = "Bangkal, Davao City", LoyaltyPoints = 510, DateRegistered = DateTime.Today.AddMonths(-11) },
                    new() { FullName = "Francis Magno", Phone = "0917-105-2005", Email = "francis.m@mail.com", Address = "Panacan, Davao City", LoyaltyPoints = 95, DateRegistered = DateTime.Today.AddMonths(-3) },
                    new() { FullName = "Christine Joy Reyes", Phone = "0917-106-2006", Email = "cj.reyes@mail.com", Address = "Ecoland, Davao City", LoyaltyPoints = 340, DateRegistered = DateTime.Today.AddMonths(-6) },
                    new() { FullName = "Dennis Morales", Phone = "0917-107-2007", Email = "dennis.m@mail.com", Address = "Maa, Davao City", LoyaltyPoints = 620, DateRegistered = DateTime.Today.AddYears(-1) },
                    new() { FullName = "Patricia Ann Mercado", Phone = "0917-108-2008", Email = "patricia.m@mail.com", Address = "Tibungco, Davao City", LoyaltyPoints = 150, DateRegistered = DateTime.Today.AddMonths(-4) },
                    new() { FullName = "Rodrigo Pascual", Phone = "0917-109-2009", Email = "rodrigo.p@mail.com", Address = "Sasa, Davao City", LoyaltyPoints = 210, DateRegistered = DateTime.Today.AddMonths(-5) },
                    new() { FullName = "Angelica Soriano", Phone = "0917-110-2010", Email = "angelica.s@mail.com", Address = "Bago Aplaya, Davao City", LoyaltyPoints = 480, DateRegistered = DateTime.Today.AddMonths(-9) },
                    new() { FullName = "Kevin Christian Ong", Phone = "0917-111-2011", Email = "kevin.ong@mail.com", Address = "Obrero, Davao City", LoyaltyPoints = 730, DateRegistered = DateTime.Today.AddYears(-2) },
                    new() { FullName = "Teresa Guinto", Phone = "0917-112-2012", Email = "teresa.g@mail.com", Address = "Mabini, Davao City", LoyaltyPoints = 135, DateRegistered = DateTime.Today.AddMonths(-3) },
                    new() { FullName = "Jonathan Pineda", Phone = "0917-113-2013", Email = "jonathan.p@mail.com", Address = "Mintal, Davao City", LoyaltyPoints = 290, DateRegistered = DateTime.Today.AddMonths(-6) },
                    new() { FullName = "Katrina Mae Roxas", Phone = "0917-114-2014", Email = "katrina.roxas@mail.com", Address = "Catalunan Grande, Davao City", LoyaltyPoints = 375, DateRegistered = DateTime.Today.AddMonths(-7) },
                    new() { FullName = "Victoriano Cruz", Phone = "0917-115-2015", Email = "victor.cruz@mail.com", Address = "Buhangin, Davao City", LoyaltyPoints = 80, DateRegistered = DateTime.Today.AddMonths(-2) },
                    new() { FullName = "Bea Bianca Flores", Phone = "0917-116-2016", Email = "bea.flores@mail.com", Address = "Ulas, Davao City", LoyaltyPoints = 220, DateRegistered = DateTime.Today.AddMonths(-4) },
                    new() { FullName = "Carlos Miguel Santos", Phone = "0917-117-2017", Email = "carlos.santos@mail.com", Address = "Matina Aplaya, Davao City", LoyaltyPoints = 640, DateRegistered = DateTime.Today.AddYears(-1) },
                    new() { FullName = "Stephanie Nicole Yu", Phone = "0917-118-2018", Email = "steph.yu@mail.com", Address = "Poblacion, Davao City", LoyaltyPoints = 430, DateRegistered = DateTime.Today.AddMonths(-8) },
                    new() { FullName = "Ferdinand Ramos", Phone = "0917-119-2019", Email = "f.ramos@mail.com", Address = "Mandug, Davao City", LoyaltyPoints = 190, DateRegistered = DateTime.Today.AddMonths(-5) },
                    new() { FullName = "Kimberly Anne Diaz", Phone = "0917-120-2020", Email = "kim.diaz@mail.com", Address = "Tigatto, Davao City", LoyaltyPoints = 310, DateRegistered = DateTime.Today.AddMonths(-6) },
                    new() { FullName = "Arthur James Cortez", Phone = "0917-121-2021", Email = "arthur.c@mail.com", Address = "Indangan, Davao City", LoyaltyPoints = 550, DateRegistered = DateTime.Today.AddMonths(-10) },
                    new() { FullName = "Melissa Jane Alcantara", Phone = "0917-122-2022", Email = "melissa.a@mail.com", Address = "Cabantian, Davao City", LoyaltyPoints = 160, DateRegistered = DateTime.Today.AddMonths(-3) },
                    new() { FullName = "Rafael Sebastian Go", Phone = "0917-123-2023", Email = "rafael.go@mail.com", Address = "Buhangin, Davao City", LoyaltyPoints = 820, DateRegistered = DateTime.Today.AddYears(-2) },
                    new() { FullName = "Lorraine Mae Torres", Phone = "0917-124-2024", Email = "lorraine.t@mail.com", Address = "Bunawan, Davao City", LoyaltyPoints = 110, DateRegistered = DateTime.Today.AddMonths(-2) },
                    new() { FullName = "Emmanuel David Navarro", Phone = "0917-125-2025", Email = "e.navarro@mail.com", Address = "Buhangin, Davao City", LoyaltyPoints = 275, DateRegistered = DateTime.Today.AddMonths(-5) },
                    new() { FullName = "Clarisse Anne Mendoza", Phone = "0917-126-2026", Email = "clarisse.m@mail.com", Address = "Marfori Heights, Davao City", LoyaltyPoints = 490, DateRegistered = DateTime.Today.AddMonths(-9) }
                };

                context.Customers.AddRange(extraCustomers);
                await context.SaveChangesAsync();
            }

            // 4. Ensure Prescriptions (at least 25 prescriptions)
            if (await context.Prescriptions.CountAsync() < 25)
            {
                var allCustomers = await context.Customers.ToListAsync();
                var allMeds = await context.Medicines.ToListAsync();
                var doctors = new[]
                {
                    "Dr. Rafael Mendoza, MD (Cardiology)",
                    "Dr. Corazon Aquino-Reyes, MD (Internal Medicine)",
                    "Dr. Arturo Gomez, MD (Endocrinology)",
                    "Dr. Maria Patricia Santos, MD (Pulmonology)",
                    "Dr. Eduardo Tan, MD (Pediatrics)",
                    "Dr. Liza Marie Soriano, MD (Family Medicine)",
                    "Dr. Benjamin Cruz, MD (Gastroenterology)",
                    "Dr. Carmela Bautista, MD (Dermatology / Allergy)"
                };

                var rxList = new List<Prescription>();
                for (int i = 1; i <= 24; i++)
                {
                    var cust = allCustomers[(i * 3) % allCustomers.Count];
                    var doc = doctors[i % doctors.Length];
                    var med = allMeds[(i * 2) % allMeds.Count];
                    rxList.Add(new Prescription
                    {
                        CustomerId = cust.Id,
                        DoctorName = doc,
                        DatePrescribed = DateTime.Today.AddDays(-((i * 5) % 45)),
                        Status = (i % 3 == 0 ? PrescriptionStatus.Pending : PrescriptionStatus.Fulfilled),
                        Details = new List<PrescriptionDetail>
                        {
                            new() { MedicineId = med.Id, Quantity = 20 + (i * 2), Dosage = "Take 1 dose daily as advised by attending physician." }
                        }
                    });
                }
                context.Prescriptions.AddRange(rxList);
                await context.SaveChangesAsync();
            }

            // 5. Ensure Billings & Sales (at least 65 records)
            int currentBillings = await context.Billings.CountAsync();
            if (currentBillings < 65)
            {
                int needed = 65 - currentBillings;
                var allCustomers = await context.Customers.ToListAsync();
                var allMeds = await context.Medicines.Include(m => m.Batches).Where(m => m.Batches.Any()).ToListAsync();
                var cashierUser = await userManager.FindByEmailAsync("cashier@careplus.ph");
                var cashierId = cashierUser?.Id;

                var paymentMethods = new[] { "Cash", "GCash", "Maya", "Credit Card", "Debit Card" };

                for (int i = 1; i <= needed; i++)
                {
                    int invNum = currentBillings + i;
                    var saleDate = DateTime.Today.AddDays(-((i * 11) % 42)).AddHours(8 + (i * 4) % 11).AddMinutes((i * 19) % 60);

                    // Pick customer (10% walk-in)
                    Customer? customer = (i % 9 == 0) ? null : allCustomers[(i * 5) % allCustomers.Count];
                    int? customerId = customer?.Id;

                    // Pick 1 to 3 items
                    var saleDetails = new List<SaleDetail>();
                    decimal gross = 0;

                    int itemCount = 1 + (i % 3);
                    for (int j = 0; j < itemCount; j++)
                    {
                        var med = allMeds[(i * 3 + j * 7) % allMeds.Count];
                        var batch = med.Batches.FirstOrDefault();
                        int qty = (med.UnitPrice > 100 ? (1 + (i % 3)) : (10 + (i * 2) % 20));
                        decimal unitPrice = med.UnitPrice;

                        saleDetails.Add(new SaleDetail
                        {
                            MedicineId = med.Id,
                            BatchId = batch?.Id,
                            Quantity = qty,
                            UnitPrice = unitPrice
                        });

                        gross += qty * unitPrice;
                    }

                    string payMethod = paymentMethods[i % paymentMethods.Length];
                    decimal discount = (customer != null && customer.LoyaltyPoints > 50 && i % 4 == 0) ? Math.Min(50, gross * 0.1m) : 0;
                    decimal netAmount = Math.Max(10, gross - discount);
                    decimal paidAmount = (payMethod == "Cash") ? Math.Ceiling(netAmount / 50m) * 50m : netAmount;
                    decimal change = Math.Max(0, paidAmount - netAmount);

                    var sale = new Sale
                    {
                        CustomerId = customerId,
                        CashierId = cashierId,
                        PaymentMethod = payMethod,
                        SaleDate = saleDate,
                        DiscountAmount = discount,
                        PointsEarned = (int)(netAmount / 100),
                        PointsRedeemed = (int)discount,
                        Details = saleDetails
                    };

                    context.Sales.Add(sale);
                    await context.SaveChangesAsync();

                    var billing = new Billing
                    {
                        SaleId = sale.Id,
                        InvoiceNumber = $"INV-{saleDate.Year}-{invNum:D5}",
                        AmountDue = netAmount,
                        AmountPaid = paidAmount,
                        ChangeAmount = change,
                        PaymentMethod = payMethod,
                        PaymentStatus = (i % 16 == 0 ? PaymentStatus.Unpaid : PaymentStatus.Paid),
                        DateIssued = saleDate
                    };

                    context.Billings.Add(billing);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
