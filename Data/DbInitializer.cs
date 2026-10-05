using Microsoft.EntityFrameworkCore;
using LibraTrack.Models;

namespace LibraTrack.Data
{
    public static class DbInitializer
    {
        public static void Initialize(LibraryDbContext context, bool resetDatabase = false)
        {
            // If requested (e.g. in development mode), delete old database to rebuild schema with newly added tables
            if (resetDatabase)
            {
                context.Database.EnsureDeleted();
            }

            // Ensure database and all tables are created
            context.Database.EnsureCreated();

            // Check if users already seeded
            if (context.Users.Any())
            {
                return; // DB has already been seeded
            }

            // ==========================================
            // 1. SEED STAFF USERS
            // ==========================================
            var users = new List<UserAccount>
            {
                new UserAccount
                {
                    Username = "admin",
                    Email = "admin@library.com",
                    FullName = "Eleanor Vance (Chief Administrator)",
                    Role = UserRole.Admin,
                    PasswordHash = UserAccount.HashPassword("Admin@123"),
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new UserAccount
                {
                    Username = "reception",
                    Email = "reception@library.com",
                    FullName = "Julian Hayes (Front Desk Officer)",
                    Role = UserRole.Reception,
                    PasswordHash = UserAccount.HashPassword("Reception@123"),
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new UserAccount
                {
                    Username = "manager",
                    Email = "manager@library.com",
                    FullName = "Dr. Marcus Thorne (Operations Director)",
                    Role = UserRole.Manager,
                    PasswordHash = UserAccount.HashPassword("Manager@123"),
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // ==========================================
            // 2. SEED BORROWERS
            // ==========================================
            var borrowers = new List<Borrower>
            {
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1001",
                    FirstName = "Alice",
                    LastName = "Morgan",
                    Email = "alice.morgan@example.com",
                    Phone = "+61 412 345 678",
                    Address = "14 Elm Street, Darwin, NT 0800",
                    MembershipDate = DateTime.UtcNow.AddMonths(-10),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1002",
                    FirstName = "David",
                    LastName = "Chen",
                    Email = "david.chen@example.com",
                    Phone = "+61 423 456 789",
                    Address = "88 Casuarina Drive, Nightcliff, NT 0810",
                    MembershipDate = DateTime.UtcNow.AddMonths(-8),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1003",
                    FirstName = "Sophia",
                    LastName = "Taylor",
                    Email = "sophia.taylor@example.com",
                    Phone = "+61 434 567 890",
                    Address = "23 Mitchell Street, Darwin, NT 0800",
                    MembershipDate = DateTime.UtcNow.AddMonths(-7),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1004",
                    FirstName = "Liam",
                    LastName = "O'Connor",
                    Email = "liam.oconnor@example.com",
                    Phone = "+61 445 678 901",
                    Address = "5 Stuart Highway, Stuart Park, NT 0820",
                    MembershipDate = DateTime.UtcNow.AddMonths(-5),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1005",
                    FirstName = "Emma",
                    LastName = "Watson",
                    Email = "emma.watson@example.com",
                    Phone = "+61 456 789 012",
                    Address = "72 Nightcliff Road, Rapid Creek, NT 0810",
                    MembershipDate = DateTime.UtcNow.AddMonths(-4),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1006",
                    FirstName = "Noah",
                    LastName = "Patel",
                    Email = "noah.patel@example.com",
                    Phone = "+61 467 890 123",
                    Address = "31 Progress Drive, Coconut Grove, NT 0810",
                    MembershipDate = DateTime.UtcNow.AddMonths(-3),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1007",
                    FirstName = "Grace",
                    LastName = "Kim",
                    Email = "grace.kim@example.com",
                    Phone = "+61 478 901 234",
                    Address = "9 Fannie Bay Place, Fannie Bay, NT 0820",
                    MembershipDate = DateTime.UtcNow.AddMonths(-2),
                    IsActive = true
                },
                new Borrower
                {
                    BorrowerCardNumber = "BRW-1008",
                    FirstName = "Lucas",
                    LastName = "Miller",
                    Email = "lucas.miller@example.com",
                    Phone = "+61 489 012 345",
                    Address = "104 Dick Ward Drive, Millner, NT 0810",
                    MembershipDate = DateTime.UtcNow.AddMonths(-1),
                    IsActive = true
                }
            };
            foreach (var b in borrowers)
            {
                // Default 4-digit PIN for self-service kiosk: "1234"
                b.PinHash = Borrower.HashPin("1234");
                b.FailedPinAttempts = 0;
                b.IsLockedOut = false;
            }
            context.Borrowers.AddRange(borrowers);
            context.SaveChanges();

            // ==========================================
            // 3. SEED BOOKS
            // ==========================================
            var books = new List<Book>
            {
                new Book
                {
                    LibraryCode = "BOK-1001",
                    Name = "The Midnight Library",
                    Description = "Between life and death there is a library with infinite shelves of books, each telling the story of what your life would look like if you made different choices. Matt Haig's beloved fantasy about possibility, regret, and finding hope.",
                    Status = ItemStatus.Available,
                    Author = "Matt Haig",
                    Genre = "Contemporary Fiction & Fantasy",
                    ISBN = "978-0525559474",
                    PageCount = 304,
                    Publisher = "Canongate Books",
                    PublicationYear = 2020,
                    ReplacementCost = 24.99m,
                    ImageUrl = "/images/items/midnight_library.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-8)
                },
                new Book
                {
                    LibraryCode = "BOK-1002",
                    Name = "Atomic Habits: Tiny Changes, Remarkable Results",
                    Description = "The definitive guide to breaking bad behaviors and adopting good ones in four simple steps. James Clear reveals practical strategies on how small daily changes compound into massive life transformations.",
                    Status = ItemStatus.Borrowed,
                    Author = "James Clear",
                    Genre = "Self-Improvement & Psychology",
                    ISBN = "978-0735211292",
                    PageCount = 320,
                    Publisher = "Avery / Penguin",
                    PublicationYear = 2018,
                    ReplacementCost = 28.00m,
                    ImageUrl = "/images/items/atomic_habits.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-8)
                },
                new Book
                {
                    LibraryCode = "BOK-1003",
                    Name = "Dune: Chronicles of Arrakis",
                    Description = "Frank Herbert's monumental science fiction masterpiece set on the harsh desert planet Arrakis. A sweeping saga of political intrigue, mysticism, ecology, and the destiny of young Paul Atreides.",
                    Status = ItemStatus.Available,
                    Author = "Frank Herbert",
                    Genre = "Science Fiction",
                    ISBN = "978-0441172719",
                    PageCount = 688,
                    Publisher = "Chilton Books",
                    PublicationYear = 1965,
                    ReplacementCost = 29.50m,
                    ImageUrl = "/images/items/dune.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-7)
                },
                new Book
                {
                    LibraryCode = "BOK-1004",
                    Name = "Designing Data-Intensive Applications",
                    Description = "Martin Kleppmann's acclaimed technical guide on distributed systems, data models, storage engines, replication, partitioning, transactions, and stream processing for modern software architects.",
                    Status = ItemStatus.Available,
                    Author = "Martin Kleppmann",
                    Genre = "Computer Systems & Architecture",
                    ISBN = "978-1449373320",
                    PageCount = 616,
                    Publisher = "O'Reilly Media",
                    PublicationYear = 2017,
                    ReplacementCost = 59.99m,
                    ImageUrl = "/images/items/data_intensive.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Book
                {
                    LibraryCode = "BOK-1005",
                    Name = "Sapiens: A Brief History of Humankind",
                    Description = "Yuval Noah Harari explores 70,000 years of human evolution, from obscure foraging apes to rulers of planet Earth, examining how cognitive, agricultural, and scientific revolutions shaped our species.",
                    Status = ItemStatus.Borrowed,
                    Author = "Yuval Noah Harari",
                    Genre = "Anthropology & History",
                    ISBN = "978-0062316097",
                    PageCount = 443,
                    Publisher = "Harper",
                    PublicationYear = 2014,
                    ReplacementCost = 34.50m,
                    ImageUrl = "/images/items/sapiens.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                },
                new Book
                {
                    LibraryCode = "BOK-1006",
                    Name = "The Silent Patient",
                    Description = "Alicia Berenson's life is seemingly perfect until one night she shoots her husband five times in the face and never speaks another word. Theo Faber, a forensic psychotherapist, attempts to uncover her motive.",
                    Status = ItemStatus.Available,
                    Author = "Alex Michaelides",
                    Genre = "Psychological Thriller",
                    ISBN = "978-1250301696",
                    PageCount = 336,
                    Publisher = "Celadon Books",
                    PublicationYear = 2019,
                    ReplacementCost = 22.00m,
                    ImageUrl = "/images/items/silent_patient.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Book
                {
                    LibraryCode = "BOK-1007",
                    Name = "Pride and Prejudice",
                    Description = "Jane Austen's timeless romantic comedy of manners detailing the turbulent relationship between the spirited Elizabeth Bennet and the wealthy, reserved Fitzwilliam Darcy.",
                    Status = ItemStatus.Damaged,
                    Author = "Jane Austen",
                    Genre = "Classic Literature & Romance",
                    ISBN = "978-0141439518",
                    PageCount = 432,
                    Publisher = "Penguin Classics",
                    PublicationYear = 1813,
                    ReplacementCost = 18.50m,
                    ImageUrl = "/images/items/pride_prejudice.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                }
            };
            for (int i = 0; i < books.Count; i++)
            {
                books[i].BranchId = branches[i % branches.Count].Id;
            }
            context.Books.AddRange(books);
            context.SaveChanges();

            // ==========================================
            // 4. SEED MUSIC
            // ==========================================
            var musicList = new List<Music>
            {
                new Music
                {
                    LibraryCode = "MUS-2001",
                    Name = "Abbey Road (50th Anniversary Remaster)",
                    Description = "The landmark 1969 studio album by The Beatles featuring 'Come Together', 'Something', 'Here Comes the Sun', and the celebrated Side Two symphonic medley in 180g heavyweight vinyl.",
                    Status = ItemStatus.Available,
                    Artist = "The Beatles",
                    ReleaseYear = 1969,
                    RecordLabel = "Apple / EMI",
                    Format = "Vinyl",
                    DurationMinutes = 47,
                    Genre = "Classic Rock",
                    ReplacementCost = 48.00m,
                    ImageUrl = "/images/items/abbey_road.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-8)
                },
                new Music
                {
                    LibraryCode = "MUS-2002",
                    Name = "Kind of Blue (Master Sound Audiophile Edition)",
                    Description = "Miles Davis's historic 1959 modal jazz masterpiece featuring John Coltrane, Cannonball Adderley, Bill Evans, and Paul Chambers. Regarded as the greatest jazz recording of all time.",
                    Status = ItemStatus.Borrowed,
                    Artist = "Miles Davis",
                    ReleaseYear = 1959,
                    RecordLabel = "Columbia Master Sound",
                    Format = "Vinyl",
                    DurationMinutes = 46,
                    Genre = "Modal Jazz",
                    ReplacementCost = 45.00m,
                    ImageUrl = "/images/items/kind_of_blue.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-7)
                },
                new Music
                {
                    LibraryCode = "MUS-2003",
                    Name = "Random Access Memories",
                    Description = "Daft Punk's multi-Grammy winning electronic funk symphony honoring late-1970s and early-1980s analog recording, featuring Nile Rodgers, Pharrell Williams, and Giorgio Moroder.",
                    Status = ItemStatus.Available,
                    Artist = "Daft Punk",
                    ReleaseYear = 2013,
                    RecordLabel = "Columbia Records",
                    Format = "Vinyl",
                    DurationMinutes = 74,
                    Genre = "Electronic / Funk / Disco",
                    ReplacementCost = 52.00m,
                    ImageUrl = "/images/items/daft_punk.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Music
                {
                    LibraryCode = "MUS-2004",
                    Name = "The Dark Side of the Moon",
                    Description = "Pink Floyd's conceptual progressive rock milestone exploring time, greed, conflict, and mental stability, with legendary production and sound design by Alan Parsons at Abbey Road Studios.",
                    Status = ItemStatus.Available,
                    Artist = "Pink Floyd",
                    ReleaseYear = 1973,
                    RecordLabel = "Harvest Records",
                    Format = "Vinyl",
                    DurationMinutes = 43,
                    Genre = "Progressive Rock",
                    ReplacementCost = 50.00m,
                    ImageUrl = "/images/items/dark_side_moon.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                },
                new Music
                {
                    LibraryCode = "MUS-2005",
                    Name = "Rumours",
                    Description = "Fleetwood Mac's celebrated pop-rock triumph composed amidst intense interpersonal relationships, containing unforgettable anthems 'Dreams', 'Go Your Own Way', and 'The Chain'.",
                    Status = ItemStatus.Damaged,
                    Artist = "Fleetwood Mac",
                    ReleaseYear = 1977,
                    RecordLabel = "Warner Records",
                    Format = "Vinyl",
                    DurationMinutes = 40,
                    Genre = "Soft Rock",
                    ReplacementCost = 42.00m,
                    ImageUrl = "/images/items/fleetwood_mac.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Music
                {
                    LibraryCode = "MUS-2006",
                    Name = "Blue Train (Remastered Edition)",
                    Description = "John Coltrane's 1957 hard bop session for Blue Note with Lee Morgan, Curtis Fuller, Kenny Drew, Paul Chambers, and Philly Joe Jones. A pinnacle of modern American jazz.",
                    Status = ItemStatus.Available,
                    Artist = "John Coltrane",
                    ReleaseYear = 1957,
                    RecordLabel = "Blue Note Records",
                    Format = "CD",
                    DurationMinutes = 42,
                    Genre = "Hard Bop Jazz",
                    ReplacementCost = 26.00m,
                    ImageUrl = "/images/items/coltrane_jazz.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new Music
                {
                    LibraryCode = "MUS-2007",
                    Name = "Beethoven: Symphony No. 9 in D Minor, Op. 125",
                    Description = "Herbert von Karajan leads the Berlin Philharmonic in a powerful, definitive recording of Beethoven's monumental Choral Symphony culminating in the 'Ode to Joy'.",
                    Status = ItemStatus.Available,
                    Artist = "Ludwig van Beethoven / Berlin Philharmonic",
                    ReleaseYear = 1963,
                    RecordLabel = "Deutsche Grammophon",
                    Format = "CD",
                    DurationMinutes = 67,
                    Genre = "Classical Orchestral",
                    ReplacementCost = 30.00m,
                    ImageUrl = "/images/items/beethoven_symphony.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                }
            };
            for (int i = 0; i < musicList.Count; i++)
            {
                musicList[i].BranchId = branches[(i + 1) % branches.Count].Id;
            }
            context.MusicAlbums.AddRange(musicList);
            context.SaveChanges();

            // ==========================================
            // 5. SEED TOYS & GAMES
            // ==========================================
            var toys = new List<Toy>
            {
                new Toy
                {
                    LibraryCode = "TOY-3001",
                    Name = "LEGO Ideas: NASA Apollo Saturn V Rocket",
                    Description = "Majestic meter-high 1:110 scale model of NASA's historic lunar exploration rocket. Features 3 removable rocket stages, lunar lander with micro astronauts, and splashdown rescue craft.",
                    Status = ItemStatus.Available,
                    ToyType = "STEM & Space Construction",
                    RecommendedAgeMin = 14,
                    RecommendedAgeMax = 99,
                    Brand = "LEGO",
                    PieceCount = 1969,
                    SafetyNotes = "Contains small interlocking bricks. Not suitable for children under 3 years.",
                    ReplacementCost = 169.00m,
                    ImageUrl = "/images/items/lego_saturn_v.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-8)
                },
                new Toy
                {
                    LibraryCode = "TOY-3002",
                    Name = "Settlers of Catan: 5th Edition",
                    Description = "The world-famous strategy board game of resource gathering, tactical trading, settlement expansion, and road building on the modular island of Catan.",
                    Status = ItemStatus.Borrowed,
                    ToyType = "Strategy Board Game",
                    RecommendedAgeMin = 10,
                    RecommendedAgeMax = 99,
                    Brand = "Catan Studio",
                    PieceCount = 120,
                    SafetyNotes = "Contains dice, wood tokens and resource cards. Choking hazard for toddlers.",
                    ReplacementCost = 55.00m,
                    ImageUrl = "/images/items/catan.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-7)
                },
                new Toy
                {
                    LibraryCode = "TOY-3003",
                    Name = "Handcrafted Grandmaster Wooden Chess & Checkers Set",
                    Description = "Solid walnut and Canadian maple folding chess board with weighted Staunton wood pieces, felt bases, brass lock clasps, and velvet storage compartments.",
                    Status = ItemStatus.Available,
                    ToyType = "Classic Strategy Game",
                    RecommendedAgeMin = 6,
                    RecommendedAgeMax = 99,
                    Brand = "Staunton Heritage",
                    PieceCount = 56,
                    SafetyNotes = "Solid wood pieces; store securely in locked clasps.",
                    ReplacementCost = 85.00m,
                    ImageUrl = "/images/items/wooden_chess.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Toy
                {
                    LibraryCode = "TOY-3004",
                    Name = "Celestron FirstScope Astronomical Telescope",
                    Description = "High-quality 76mm reflector optical telescope with Dobsonian tabletop mount, two Kellner eyepieces (20mm and 4mm), and rotating Star Map Guidebook for young astronomers.",
                    Status = ItemStatus.Available,
                    ToyType = "Science & Astronomy Kit",
                    RecommendedAgeMin = 8,
                    RecommendedAgeMax = 16,
                    Brand = "Celestron",
                    PieceCount = 12,
                    SafetyNotes = "WARNING: Never point telescope at or near the sun without solar filter.",
                    ReplacementCost = 119.00m,
                    ImageUrl = "/images/items/telescope.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                },
                new Toy
                {
                    LibraryCode = "TOY-3005",
                    Name = "Sphero BOLT Programmable Robotic STEM Ball",
                    Description = "App-enabled educational robotic ball with 8x8 LED animated matrix, infrared sensors, digital compass, gyroscope, and multi-level visual block / JavaScript coding platform.",
                    Status = ItemStatus.Damaged,
                    ToyType = "Coding & Robotics",
                    RecommendedAgeMin = 8,
                    RecommendedAgeMax = 18,
                    Brand = "Sphero",
                    PieceCount = 6,
                    SafetyNotes = "Waterproof polycarbonate shell; use inductive charging cradle.",
                    ReplacementCost = 179.00m,
                    ImageUrl = "/images/items/sphero_robot.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Toy
                {
                    LibraryCode = "TOY-3006",
                    Name = "Wingspan Board Game",
                    Description = "Award-winning competitive, medium-weight, card-driven, engine-building board game about bird enthusiasts, ornithologists, and habitat conservationists.",
                    Status = ItemStatus.Available,
                    ToyType = "Engine-Building Board Game",
                    RecommendedAgeMin = 10,
                    RecommendedAgeMax = 99,
                    Brand = "Stonemaier Games",
                    PieceCount = 210,
                    SafetyNotes = "Contains wooden egg tokens and food dice.",
                    ReplacementCost = 65.00m,
                    ImageUrl = "/images/items/wingspan.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new Toy
                {
                    LibraryCode = "TOY-3007",
                    Name = "Ravensburger 1000-Piece Ancient World Map Puzzle",
                    Description = "Stunning historical cartography puzzle featuring Softclick Technology, matte anti-glare finish, and precision interlocking linen-embossed pieces.",
                    Status = ItemStatus.Available,
                    ToyType = "Jigsaw Puzzle",
                    RecommendedAgeMin = 12,
                    RecommendedAgeMax = 99,
                    Brand = "Ravensburger",
                    PieceCount = 1000,
                    SafetyNotes = "Small puzzle pieces; not suitable for children under 3.",
                    ReplacementCost = 28.00m,
                    ImageUrl = "/images/items/ravensburger_puzzle.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                }
            };
            for (int i = 0; i < toys.Count; i++)
            {
                toys[i].BranchId = branches[(i + 2) % branches.Count].Id;
            }
            context.Toys.AddRange(toys);
            context.SaveChanges();

            // ==========================================
            // 6. SEED LOANS & TRANSACTIONS
            // ==========================================
            var allBooks = context.Books.ToList();
            var allMusic = context.MusicAlbums.ToList();
            var allToys = context.Toys.ToList();
            var allBorrowers = context.Borrowers.ToList();

            var loans = new List<Loan>
            {
                // Past returned loans
                new Loan
                {
                    ItemId = allBooks[0].Id, // Midnight Library
                    BorrowerId = allBorrowers[0].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-180),
                    DueDate = DateTime.UtcNow.AddDays(-166),
                    ReturnDate = DateTime.UtcNow.AddDays(-167),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good,
                    Notes = "Returned in pristine condition."
                },
                new Loan
                {
                    ItemId = allMusic[0].Id, // Abbey Road
                    BorrowerId = allBorrowers[1].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-175),
                    DueDate = DateTime.UtcNow.AddDays(-161),
                    ReturnDate = DateTime.UtcNow.AddDays(-158),
                    FineAmount = 4.50m, // 3 days overdue ($1.50/day)
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good,
                    Notes = "Overdue fine of $4.50 paid at desk."
                },
                new Loan
                {
                    ItemId = allBooks[2].Id, // Dune
                    BorrowerId = allBorrowers[2].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-150),
                    DueDate = DateTime.UtcNow.AddDays(-136),
                    ReturnDate = DateTime.UtcNow.AddDays(-135),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allToys[2].Id, // Wooden Chess
                    BorrowerId = allBorrowers[3].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-145),
                    DueDate = DateTime.UtcNow.AddDays(-131),
                    ReturnDate = DateTime.UtcNow.AddDays(-130),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allBooks[0].Id, // Midnight Library (2nd borrow)
                    BorrowerId = allBorrowers[4].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-120),
                    DueDate = DateTime.UtcNow.AddDays(-106),
                    ReturnDate = DateTime.UtcNow.AddDays(-104),
                    FineAmount = 3.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allMusic[3].Id, // Dark Side of the Moon
                    BorrowerId = allBorrowers[0].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-115),
                    DueDate = DateTime.UtcNow.AddDays(-101),
                    ReturnDate = DateTime.UtcNow.AddDays(-100),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allToys[0].Id, // Lego Saturn V
                    BorrowerId = allBorrowers[1].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-110),
                    DueDate = DateTime.UtcNow.AddDays(-96),
                    ReturnDate = DateTime.UtcNow.AddDays(-95),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allBooks[3].Id, // Data-Intensive Applications
                    BorrowerId = allBorrowers[5].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-90),
                    DueDate = DateTime.UtcNow.AddDays(-76),
                    ReturnDate = DateTime.UtcNow.AddDays(-75),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allBooks[6].Id, // Pride & Prejudice (Returned Damaged)
                    BorrowerId = allBorrowers[6].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-85),
                    DueDate = DateTime.UtcNow.AddDays(-71),
                    ReturnDate = DateTime.UtcNow.AddDays(-69),
                    FineAmount = 3.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Damaged,
                    Notes = "Creased spine and small liquid stain on corner."
                },
                new Loan
                {
                    ItemId = allMusic[2].Id, // Daft Punk
                    BorrowerId = allBorrowers[7].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-80),
                    DueDate = DateTime.UtcNow.AddDays(-66),
                    ReturnDate = DateTime.UtcNow.AddDays(-65),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allBooks[0].Id, // Midnight Library (3rd borrow)
                    BorrowerId = allBorrowers[2].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-60),
                    DueDate = DateTime.UtcNow.AddDays(-46),
                    ReturnDate = DateTime.UtcNow.AddDays(-45),
                    FineAmount = 0.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Good
                },
                new Loan
                {
                    ItemId = allMusic[4].Id, // Fleetwood Mac (Returned Damaged)
                    BorrowerId = allBorrowers[1].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-28),
                    DueDate = DateTime.UtcNow.AddDays(-14),
                    ReturnDate = DateTime.UtcNow.AddDays(-10),
                    FineAmount = 6.00m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Damaged,
                    Notes = "Scratch on Side B track 2."
                },
                new Loan
                {
                    ItemId = allToys[4].Id, // Sphero Robot (Returned Damaged)
                    BorrowerId = allBorrowers[2].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-25),
                    DueDate = DateTime.UtcNow.AddDays(-11),
                    ReturnDate = DateTime.UtcNow.AddDays(-10),
                    FineAmount = 1.50m,
                    FinePaid = true,
                    ReturnedCondition = ReturnCondition.Damaged,
                    Notes = "Scratched outer casing; electronics functional."
                },

                // ACTIVE CURRENT LOANS
                // 1. Atomic Habits borrowed by Alice Morgan - Active (on time)
                new Loan
                {
                    ItemId = allBooks[1].Id,
                    BorrowerId = allBorrowers[0].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-5),
                    DueDate = DateTime.UtcNow.AddDays(9),
                    ReturnDate = null,
                    FineAmount = 0.00m,
                    FinePaid = false,
                    Notes = "Standard 14-day checkout."
                },
                // 2. Sapiens borrowed by David Chen - Active (OVERDUE by 4 days)
                new Loan
                {
                    ItemId = allBooks[4].Id,
                    BorrowerId = allBorrowers[1].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-18),
                    DueDate = DateTime.UtcNow.AddDays(-4),
                    ReturnDate = null,
                    FineAmount = 6.00m, // 4 days * $1.50
                    FinePaid = false,
                    Notes = "Reminder notification sent."
                },
                // 3. Kind of Blue borrowed by Sophia Taylor - Active (on time)
                new Loan
                {
                    ItemId = allMusic[1].Id,
                    BorrowerId = allBorrowers[2].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(11),
                    ReturnDate = null,
                    FineAmount = 0.00m,
                    FinePaid = false,
                    Notes = "Audiophile vinyl in protective sleeve."
                },
                // 4. Catan borrowed by Liam O'Connor - Active (OVERDUE by 2 days)
                new Loan
                {
                    ItemId = allToys[1].Id,
                    BorrowerId = allBorrowers[3].Id,
                    BorrowDate = DateTime.UtcNow.AddDays(-16),
                    DueDate = DateTime.UtcNow.AddDays(-2),
                    ReturnDate = null,
                    FineAmount = 3.00m, // 2 days * $1.50
                    FinePaid = false,
                    Notes = "Checked out with all 120 pieces verified."
                }
            };

            context.Loans.AddRange(loans);
            context.SaveChanges();

            // ==========================================
            // 7. SEED ITEM TRANSFERS
            // ==========================================
            var allItems = context.Items.ToList();
            var adminUser = context.Users.FirstOrDefault(u => u.Role == UserRole.Admin);

            var transfers = new List<ItemTransfer>
            {
                new ItemTransfer
                {
                    ItemId = allItems[0].Id,
                    FromBranchId = allItems[0].BranchId,
                    ToBranchId = branches.First(b => b.Id != allItems[0].BranchId).Id,
                    RequestedByUserId = adminUser?.Id,
                    RequestedAt = DateTime.UtcNow.AddDays(-2),
                    Status = TransferStatus.InTransit,
                    Notes = "Inter-branch transfer to fulfill patron hold request."
                },
                new ItemTransfer
                {
                    ItemId = allItems[3].Id,
                    FromBranchId = branches[1].Id,
                    ToBranchId = branches[0].Id,
                    RequestedByUserId = adminUser?.Id,
                    RequestedAt = DateTime.UtcNow.AddDays(-6),
                    Status = TransferStatus.Received,
                    CompletedAt = DateTime.UtcNow.AddDays(-3),
                    Notes = "Routine seasonal collection rotation."
                }
            };
            context.ItemTransfers.AddRange(transfers);
            context.SaveChanges();

            // ==========================================
            // 8. SEED NOTIFICATIONS (Simulated Patron Alerts)
            // ==========================================
            var seededLoans = context.Loans.Include(l => l.Item).Include(l => l.Borrower).ToList();
            var notifications = new List<Notification>
            {
                new Notification
                {
                    BorrowerId = allBorrowers[0].Id,
                    LoanId = seededLoans[0].Id,
                    Channel = NotificationChannel.Email,
                    Type = NotificationType.Borrowed,
                    Subject = $"Item Issued: {seededLoans[0].Item.Name} [{seededLoans[0].Item.LibraryCode}]",
                    Message = $"Hello {allBorrowers[0].FullName}, you have checked out '{seededLoans[0].Item.Name}' ({seededLoans[0].Item.LibraryCode}). Due date: {seededLoans[0].DueDate:dddd, dd MMMM yyyy}.",
                    Status = NotificationStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Notification
                {
                    BorrowerId = allBorrowers[1].Id,
                    LoanId = seededLoans[1].Id,
                    Channel = NotificationChannel.Email,
                    Type = NotificationType.FineIssued,
                    Subject = $"Fine Assessed: ${seededLoans[1].FineAmount:F2} for '{seededLoans[1].Item.Name}'",
                    Message = $"Hello {allBorrowers[1].FullName}, a late fine of ${seededLoans[1].FineAmount:F2} was assessed for your returned item '{seededLoans[1].Item.Name}'. Status: PAID.",
                    Status = NotificationStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Notification
                {
                    BorrowerId = allBorrowers[2].Id,
                    LoanId = seededLoans[2].Id,
                    Channel = NotificationChannel.SMS,
                    Type = NotificationType.DueSoon,
                    Subject = $"Reminder: '{seededLoans[2].Item.Name}' is due on {seededLoans[2].DueDate:dd MMM yyyy}",
                    Message = $"Hello {allBorrowers[2].FullName}, reminder that '{seededLoans[2].Item.Name}' is due in 2 days on {seededLoans[2].DueDate:dddd, dd MMM yyyy}. Please return or renew.",
                    Status = NotificationStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddHours(-18)
                },
                new Notification
                {
                    BorrowerId = allBorrowers[3].Id,
                    LoanId = seededLoans[3].Id,
                    Channel = NotificationChannel.Email,
                    Type = NotificationType.Overdue,
                    Subject = $"OVERDUE ALERT: '{seededLoans[3].Item.Name}' was due on {seededLoans[3].DueDate:dd MMM yyyy}",
                    Message = $"URGENT: Hello {allBorrowers[3].FullName}, your loan for '{seededLoans[3].Item.Name}' is now overdue. Daily late fines of $1.00/day are being accrued. Please return this item immediately.",
                    Status = NotificationStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Notification
                {
                    BorrowerId = allBorrowers[4].Id,
                    LoanId = null,
                    Channel = NotificationChannel.SMS,
                    Type = NotificationType.ReservationReady,
                    Subject = "Hold Available: 'To Kill a Mockingbird' is ready for pickup",
                    Message = $"Good news {allBorrowers[4].FullName}! Your requested book is waiting for pickup at Darwin Central Library hold shelf. Available for 3 days.",
                    Status = NotificationStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                }
            };
            context.Notifications.AddRange(notifications);
            context.SaveChanges();

            // ==========================================
            // 9. SEED RESERVATIONS (Queue & Hold Shelf)
            // ==========================================
            var reservations = new List<Reservation>
            {
                // Waiting queue on borrowed book 'Atomic Habits' (Position #1)
                new Reservation
                {
                    ItemId = allBooks[1].Id,
                    BorrowerId = allBorrowers[4].Id,
                    QueuePosition = 1,
                    Status = ReservationStatus.Waiting,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                // Waiting queue on borrowed book 'Atomic Habits' (Position #2)
                new Reservation
                {
                    ItemId = allBooks[1].Id,
                    BorrowerId = allBorrowers[5].Id,
                    QueuePosition = 2,
                    Status = ReservationStatus.Waiting,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                // Waiting queue on damaged book 'Pride and Prejudice'
                new Reservation
                {
                    ItemId = allBooks[6].Id,
                    BorrowerId = allBorrowers[7].Id,
                    QueuePosition = 1,
                    Status = ReservationStatus.Waiting,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                // Waiting queue on borrowed toy 'Catan'
                new Reservation
                {
                    ItemId = allToys[1].Id,
                    BorrowerId = allBorrowers[6].Id,
                    QueuePosition = 1,
                    Status = ReservationStatus.Waiting,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                // Ready hold on 'Random Access Memories'
                new Reservation
                {
                    ItemId = allMusic[2].Id,
                    BorrowerId = allBorrowers[3].Id,
                    QueuePosition = 0,
                    Status = ReservationStatus.Ready,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    ExpiresAt = DateTime.UtcNow.AddDays(2)
                },
                // Past Fulfilled hold on 'The Midnight Library'
                new Reservation
                {
                    ItemId = allBooks[0].Id,
                    BorrowerId = allBorrowers[2].Id,
                    QueuePosition = 0,
                    Status = ReservationStatus.Fulfilled,
                    CreatedAt = DateTime.UtcNow.AddDays(-65),
                    ReadyAt = DateTime.UtcNow.AddDays(-61),
                    ExpiresAt = DateTime.UtcNow.AddDays(-58)
                }
            };
            context.Reservations.AddRange(reservations);
            context.SaveChanges();

            // ==========================================
            // 9. SEED IMPORT LOGS
            // ==========================================
            if (!context.ImportLogs.Any())
            {
                var initialImportLog = new ImportLog
                {
                    UserId = users[0].Id,
                    Username = users[0].Username,
                    ImportedAt = DateTime.UtcNow.AddDays(-10),
                    Source = "CSV Upload",
                    TotalRows = 21,
                    ImportedCount = 21,
                    SkippedCount = 0,
                    ErrorCount = 0,
                    Notes = "Initial batch catalog migration from legacy system across Darwin Central, Casuarina, and Palmerston branches."
                };
                context.ImportLogs.Add(initialImportLog);
                context.SaveChanges();
            }
        }
    }
}
