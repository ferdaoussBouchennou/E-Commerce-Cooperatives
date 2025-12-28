

-- ============================================
-- SUPPRESSION DES TABLES (dans l'ordre des dépendances)
-- ============================================

IF OBJECT_ID('LivraisonSuivi', 'U') IS NOT NULL DROP TABLE LivraisonSuivi;
IF OBJECT_ID('CommandeItems', 'U') IS NOT NULL DROP TABLE CommandeItems;
IF OBJECT_ID('Commandes', 'U') IS NOT NULL DROP TABLE Commandes;
IF OBJECT_ID('PanierItems', 'U') IS NOT NULL DROP TABLE PanierItems;
IF OBJECT_ID('Paniers', 'U') IS NOT NULL DROP TABLE Paniers;
IF OBJECT_ID('AvisProduits', 'U') IS NOT NULL DROP TABLE AvisProduits;
IF OBJECT_ID('Favoris', 'U') IS NOT NULL DROP TABLE Favoris;
IF OBJECT_ID('ImagesProduits', 'U') IS NOT NULL DROP TABLE ImagesProduits;
IF OBJECT_ID('Variantes', 'U') IS NOT NULL DROP TABLE Variantes;
IF OBJECT_ID('Produits', 'U') IS NOT NULL DROP TABLE Produits;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('ZonesLivraison', 'U') IS NOT NULL DROP TABLE ZonesLivraison;
IF OBJECT_ID('ModesLivraison', 'U') IS NOT NULL DROP TABLE ModesLivraison;
IF OBJECT_ID('Adresses', 'U') IS NOT NULL DROP TABLE Adresses;
IF OBJECT_ID('Clients', 'U') IS NOT NULL DROP TABLE Clients;
IF OBJECT_ID('Utilisateurs', 'U') IS NOT NULL DROP TABLE Utilisateurs;
IF OBJECT_ID('Cooperatives', 'U') IS NOT NULL DROP TABLE Cooperatives;
GO

-- ============================================
-- CRÉATION DES TABLES
-- ============================================

-- ============================================
-- TABLE UTILISATEURS ET AUTHENTIFICATION
-- ============================================

CREATE TABLE Utilisateurs (
    UtilisateurId INT PRIMARY KEY IDENTITY(1,1),
    Email NVARCHAR(255) UNIQUE NOT NULL,
    MotDePasse NVARCHAR(255) NOT NULL, 
    TypeUtilisateur NVARCHAR(20) NOT NULL CHECK (TypeUtilisateur IN ('Admin', 'Client')),
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Clients (
    ClientId INT PRIMARY KEY IDENTITY(1,1),
    UtilisateurId INT UNIQUE NOT NULL FOREIGN KEY REFERENCES Utilisateurs(UtilisateurId) ON DELETE CASCADE,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Telephone NVARCHAR(20),
    DateNaissance DATE,
    TokenResetPassword NVARCHAR(255),
    DateExpirationToken DATETIME,
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE(),
    DerniereConnexion DATETIME
);
GO

CREATE TABLE Adresses (
    AdresseId INT PRIMARY KEY IDENTITY(1,1),
    ClientId INT FOREIGN KEY REFERENCES Clients(ClientId) ON DELETE CASCADE,
    AdresseComplete NVARCHAR(500) NOT NULL,
    Ville NVARCHAR(100) NOT NULL,
    CodePostal NVARCHAR(20) NOT NULL,
    Pays NVARCHAR(100) DEFAULT 'Maroc',
    EstParDefaut BIT DEFAULT 0,
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE COOPERATIVES
-- ============================================

CREATE TABLE Cooperatives (
    CooperativeId INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(200) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    Adresse NVARCHAR(500),
    Ville NVARCHAR(100),
    Telephone NVARCHAR(20),
    Logo NVARCHAR(500),
    EstActive BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE PRODUITS ET CATALOGUE
-- ============================================

CREATE TABLE Categories (
    CategorieId INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ImageUrl NVARCHAR(500),
    EstActive BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Produits (
    ProduitId INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Prix DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(500),
    CategorieId INT FOREIGN KEY REFERENCES Categories(CategorieId),
    CooperativeId INT NULL FOREIGN KEY REFERENCES Cooperatives(CooperativeId),
    StockTotal INT NOT NULL DEFAULT 0,
    SeuilAlerte INT DEFAULT 10,
    EstDisponible BIT DEFAULT 1,
    EstEnVedette BIT DEFAULT 0,
    EstNouveau BIT DEFAULT 0,
    DateCreation DATETIME DEFAULT GETDATE(),
    DateModification DATETIME
);
GO

CREATE TABLE ImagesProduits (
    ImageId INT PRIMARY KEY IDENTITY(1,1),
    ProduitId INT FOREIGN KEY REFERENCES Produits(ProduitId) ON DELETE CASCADE,
    UrlImage NVARCHAR(500) NOT NULL,
    EstPrincipale BIT DEFAULT 0,
    DateAjout DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Variantes (
    VarianteId INT PRIMARY KEY IDENTITY(1,1),
    ProduitId INT FOREIGN KEY REFERENCES Produits(ProduitId) ON DELETE CASCADE,
    Taille NVARCHAR(50),
    Couleur NVARCHAR(50),
    Stock INT NOT NULL DEFAULT 0,
    PrixSupplementaire DECIMAL(18,2) DEFAULT 0,
    SKU NVARCHAR(100),
    EstDisponible BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE PANIER
-- ============================================

CREATE TABLE Paniers (
    PanierId INT PRIMARY KEY IDENTITY(1,1),
    ClientId INT FOREIGN KEY REFERENCES Clients(ClientId) ON DELETE CASCADE,
    DateCreation DATETIME DEFAULT GETDATE(),
    DerniereModification DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE PanierItems (
    PanierItemId INT PRIMARY KEY IDENTITY(1,1),
    PanierId INT FOREIGN KEY REFERENCES Paniers(PanierId) ON DELETE CASCADE,
    ProduitId INT FOREIGN KEY REFERENCES Produits(ProduitId),
    VarianteId INT NULL FOREIGN KEY REFERENCES Variantes(VarianteId),
    Quantite INT NOT NULL CHECK (Quantite > 0),
    PrixUnitaire DECIMAL(18,2) NOT NULL,
    DateAjout DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE MODES DE LIVRAISON
-- ============================================
-- Chaque ligne = un mode de livraison avec son tarif de base
-- Standard : Tarif = 33 MAD
-- Express : Tarif = 60 MAD

CREATE TABLE ModesLivraison (
    ModeLivraisonId INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Tarif DECIMAL(18,2) NOT NULL,  -- Prix de base du mode (33 MAD pour Standard, 60 MAD pour Express)
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE ZONES DE LIVRAISON
-- ============================================
-- Délais min/max par ville et par mode de livraison

CREATE TABLE ZonesLivraison (
    ZoneLivraisonId INT PRIMARY KEY IDENTITY(1,1),
    ZoneVille NVARCHAR(100) NOT NULL,
    Supplement DECIMAL(18,2) DEFAULT 0,  -- Supplément par ville (0 pour Casablanca)
    DelaiMinStandard INT NOT NULL,  -- Délai minimum en jours pour Standard
    DelaiMaxStandard INT NOT NULL,  -- Délai maximum en jours pour Standard
    DelaiMinExpress INT NOT NULL,   -- Délai minimum en jours pour Express
    DelaiMaxExpress INT NOT NULL,   -- Délai maximum en jours pour Express
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE COMMANDES
-- ============================================

CREATE TABLE Commandes (
    CommandeId INT PRIMARY KEY IDENTITY(1,1),
    NumeroCommande NVARCHAR(50) UNIQUE NOT NULL,
    ClientId INT FOREIGN KEY REFERENCES Clients(ClientId),
    AdresseId INT FOREIGN KEY REFERENCES Adresses(AdresseId),
    ModeLivraisonId INT FOREIGN KEY REFERENCES ModesLivraison(ModeLivraisonId),
    DateCommande DATETIME DEFAULT GETDATE(),
    FraisLivraison DECIMAL(18,2) DEFAULT 0,
    TotalHT DECIMAL(18,2) NOT NULL,
    MontantTVA DECIMAL(18,2) DEFAULT 0,
    TotalTTC DECIMAL(18,2) NOT NULL,
    Statut NVARCHAR(50) DEFAULT 'Validée'
        CHECK (Statut IN ('Validée', 'Préparation', 'Expédiée', 'Livrée', 'En cours de livraison', 'Annulée')),
    Commentaire NVARCHAR(500),
    DateAnnulation DATETIME,
    RaisonAnnulation NVARCHAR(500)
);
GO

CREATE TABLE CommandeItems (
    CommandeItemId INT PRIMARY KEY IDENTITY(1,1),
    CommandeId INT FOREIGN KEY REFERENCES Commandes(CommandeId) ON DELETE CASCADE,
    ProduitId INT FOREIGN KEY REFERENCES Produits(ProduitId),
    VarianteId INT NULL FOREIGN KEY REFERENCES Variantes(VarianteId),
    Quantite INT NOT NULL CHECK (Quantite > 0),
    PrixUnitaire DECIMAL(18,2) NOT NULL,
    TotalLigne DECIMAL(18,2) NOT NULL
);
GO

-- ============================================
-- TABLE SUIVI LIVRAISON
-- ============================================

CREATE TABLE LivraisonSuivi (
    SuiviId INT PRIMARY KEY IDENTITY(1,1),
    CommandeId INT FOREIGN KEY REFERENCES Commandes(CommandeId) ON DELETE CASCADE,
    Statut NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    NumeroSuivi NVARCHAR(100),
    DateStatut DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE AVIS PRODUITS
-- ============================================

CREATE TABLE AvisProduits (
    AvisId INT PRIMARY KEY IDENTITY(1,1),
    ClientId INT FOREIGN KEY REFERENCES Clients(ClientId),
    ProduitId INT FOREIGN KEY REFERENCES Produits(ProduitId),
    Note INT NOT NULL CHECK (Note BETWEEN 1 AND 5),
    Commentaire NVARCHAR(1000),
    DateAvis DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- TABLE FAVORIS
-- ============================================

CREATE TABLE Favoris (
    FavoriId INT PRIMARY KEY IDENTITY(1,1),
    ClientId INT NOT NULL,
    ProduitId INT NOT NULL,
    DateAjout DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ClientId) REFERENCES Clients(ClientId) ON DELETE CASCADE,
    FOREIGN KEY (ProduitId) REFERENCES Produits(ProduitId) ON DELETE CASCADE,
    UNIQUE (ClientId, ProduitId)
);
GO

-- ============================================
-- INSERTION DES DONNÉES DE TEST
-- ============================================

-- ============================================
-- 1. UTILISATEURS ET CLIENTS
-- ============================================

-- Admin
INSERT INTO Utilisateurs (Email, MotDePasse, TypeUtilisateur) VALUES
('admin@multicoop.ma', 'admin123', 'Admin');

-- Clients
INSERT INTO Utilisateurs (Email, MotDePasse, TypeUtilisateur) VALUES
('ahmed.benali@gmail.com', 'password123', 'Client'),
('fatima.zahra@gmail.com', 'password123', 'Client'),
('youssef.idrissi@gmail.com', 'password123', 'Client'),
('salma.amrani@gmail.com', 'password123', 'Client'),
('mehdi.tazi@gmail.com', 'password123', 'Client');

INSERT INTO Clients (UtilisateurId, Nom, Prenom, Telephone, DateNaissance, EstActif, DerniereConnexion) VALUES
(2, 'Benali', 'Ahmed', '+212 6 12 34 56 78', '1985-03-15', 1, '2025-01-10 14:30:00'),
(3, 'El Fassi', 'Fatima Zahra', '+212 6 23 45 67 89', '1990-07-22', 1, '2025-01-12 09:15:00'),
(4, 'Idrissi', 'Youssef', '+212 6 34 56 78 90', '1988-11-05', 1, '2025-01-11 16:45:00'),
(5, 'Amrani', 'Salma', '+212 6 45 67 89 01', '1995-02-18', 1, '2025-01-09 11:20:00'),
(6, 'Tazi', 'Mehdi', '+212 6 56 78 90 12', '1992-09-30', 1, '2025-01-13 08:00:00');

-- Adresses des clients
INSERT INTO Adresses (ClientId, AdresseComplete, Ville, CodePostal, Pays, EstParDefaut) VALUES
(1, 'Appartement 12, Immeuble Yasmine, Rue Hassan II', 'Casablanca', '20000', 'Maroc', 1),
(1, 'Villa 45, Quartier Anfa', 'Casablanca', '20100', 'Maroc', 0),
(2, 'Résidence Nejma, Bloc B, Appt 5', 'Rabat', '10000', 'Maroc', 1),
(3, 'Rue Abdelkrim El Khattabi, N°78', 'Fès', '30000', 'Maroc', 1),
(4, 'Avenue Mohammed V, Imm. Al Amal, 3ème étage', 'Marrakech', '40000', 'Maroc', 1),
(5, 'Quartier Administratif, Rue 12, N°34', 'Tétouan', '93000', 'Maroc', 1);

-- ============================================
-- 2. COOPÉRATIVES
-- ============================================

INSERT INTO Cooperatives (Nom, Description, Adresse, Ville, Telephone, Logo, EstActive, DateCreation) VALUES
('Coopérative Arganière Taroudant',
    'Depuis 1998, notre coopérative de femmes produit l''huile d''argan la plus pure du Maroc. Nous travaillons avec plus de 60 femmes berbères qui perpétuent les méthodes traditionnelles de production. Nos produits sont certifiés biologiques et équitables.',
    'Douar Ait Baamrane, Route de Taroudant',
    'Taroudant',
    '+212 528 123 456',
    '/images/cooperatives/arganiere-taroudant.jpg',
    1,
    '1998-03-15'),
('Potiers de Fès',
    'Artisans potiers perpétuant les traditions séculaires de la céramique fassi. Notre atelier familial existe depuis 5 générations. Nous créons des pièces uniques en céramique émaillée selon les techniques ancestrales.',
    'Quartier des Potiers, Bab Ftouh',
    'Fès',
    '+212 535 789 012',
    '/images/cooperatives/potiers-fes.jpg',
    1,
    '1965-07-20'),
('Coopérative Apicole Atlas',
    'Miel pur des montagnes de l''Atlas, récolté de manière traditionnelle. Nos ruches sont situées à plus de 2000m d''altitude dans des zones préservées. Production 100% naturelle sans additifs ni traitement.',
    'Village d''Imlil, Haut Atlas',
    'Imlil',
    '+212 524 456 789',
    '/images/cooperatives/apicole-atlas.jpg',
    1,
    '2005-04-10'),
('Coopérative Safran Taliouine',
    'Production de safran de qualité premium dans la région de Taliouine. Notre safran est reconnu internationalement pour sa couleur intense et son arôme exceptionnel. Récolte et tri manuel pour garantir la meilleure qualité.',
    'Centre de Taliouine, Route d''Agadir',
    'Taliouine',
    '+212 528 345 678',
    '/images/cooperatives/safran-taliouine.jpg',
    1,
    '2002-09-25'),
('Tisseuses du Rif',
    'Coopérative féminine spécialisée dans le tissage traditionnel berbère. Nous créons des tapis, couvertures et textiles selon les motifs ancestraux du Rif. Chaque pièce est unique et faite à la main.',
    'Village Chefchaouen, Quartier Andalous',
    'Chefchaouen',
    '+212 539 876 543',
    '/images/cooperatives/tisseuses-rif.jpg',
    1,
    '2010-11-08'),
('Coopérative Amlou Essaouira',
    'Spécialisée dans la production d''amlou, pâte traditionnelle à base d''amandes, huile d''argan et miel. Nos produits sont 100% naturels et préparés selon les recettes traditionnelles essaouiriennes.',
    'Medina d''Essaouira, Rue Sidi Mohammed Ben Abdellah',
    'Essaouira',
    '+212 524 123 987',
    '/images/cooperatives/amlou-essaouira.jpg',
    1,
    '2008-06-12');

-- ============================================
-- 3. CATÉGORIES
-- ============================================

INSERT INTO Categories (Nom, Description, ImageUrl, EstActive) VALUES
('Cosmétiques', 'Huiles naturelles, savons et soins pour le corps', '/Content/images/categories/cosmetics.jpg', 1),
('Alimentaire', 'Miel, épices, confitures et produits du terroir', '/Content/images/categories/food.jpg', 1),
('Poterie & Céramique', 'Tagines, plats et décoration en céramique artisanale', '/Content/images/categories/pottery.jpg', 1),
('Textiles & Vannerie', 'Tapis, paniers et accessoires tissés à la main', '/Content/images/categories/textiles.jpg', 1);

-- ============================================
-- 4. PRODUITS
-- ============================================

INSERT INTO Produits (Nom, Description, Prix, ImageUrl, CategorieId, CooperativeId, StockTotal, SeuilAlerte, EstDisponible, EstEnVedette, EstNouveau) VALUES
-- Produits Cosmétiques
('Savon Noir Beldi', 'Savon noir traditionnel 100% naturel à base d''huile d''olive. Idéal pour le hammam, exfoliant et purifiant. Format 250g.', 65.00, '/Content/images/produits/soap.jpg', 1, 1, 200, 20, 1, 1, 0),
('Huile d''Argan Pure Bio', 'Huile d''argan vierge pressée à froid, certifiée biologique. Riche en vitamine E et acides gras essentiels. Flacon 100ml.', 180.00, '/Content/images/produits/argan-oil.jpg', 1, 1, 150, 15, 1, 1, 1),
('Savon d''Argan Artisanal', 'Savon artisanal enrichi à l''huile d''argan. Hydratant et nourrissant pour tous types de peaux. 100g.', 45.00, '/Content/images/produits/soap.jpg', 1, 1, 180, 20, 1, 0, 0),
-- Produits Alimentaires
('Miel de Thym Atlas', 'Miel pur de thym récolté dans le Haut Atlas. Goût intense et aromatique. Pot de 500g.', 120.00, '/Content/images/produits/honey.jpg', 2, 3, 80, 10, 1, 1, 1),
('Safran Pur Taliouine', 'Safran de qualité premium, cultivé à Taliouine. Idéal pour vos plats et pâtisseries. 1 gramme.', 95.00, '/Content/images/produits/saffron.jpg', 2, 4, 60, 5, 1, 1, 0),
('Amlou Traditionnel', 'Pâte d''amandes grillées, miel et huile d''argan. Recette traditionnelle. Pot 250g.', 85.00, '/Content/images/produits/honey.jpg', 2, 6, 100, 15, 1, 0, 0),
('Miel d''Eucalyptus', 'Miel d''eucalyptus aux propriétés apaisantes. Parfait pour les tisanes. Pot 500g.', 110.00, '/Content/images/produits/honey.jpg', 2, 3, 75, 10, 1, 0, 0),
-- Produits Poterie
('Tagine Décoré Traditionnel', 'Tagine en terre cuite décoré à la main, motifs berbères authentiques. Diamètre 30cm.', 350.00, '/Content/images/produits/tagine.jpg', 3, 2, 45, 5, 1, 1, 1),
('Plat à Couscous Fassi', 'Grand plat à couscous en céramique émaillée, décoration traditionnelle de Fès. Diamètre 35cm.', 280.00, '/Content/images/produits/tagine.jpg', 3, 2, 38, 5, 1, 0, 0),
('Set de Bols Céramique', 'Set de 6 bols en céramique artisanale, motifs géométriques. Parfait pour le thé ou les desserts.', 195.00, '/Content/images/produits/tagine.jpg', 3, 2, 52, 8, 1, 0, 0),
-- Produits Textiles
('Panier Berbère Tissé', 'Panier artisanal tissé à la main en raphia naturel. Motifs traditionnels berbères. Taille moyenne.', 220.00, '/Content/images/produits/basket.jpg', 4, 5, 35, 5, 1, 0, 1),
('Tapis Berbère Fait Main', 'Tapis 100% laine, tissé à la main selon la tradition rifaine. Motifs géométriques uniques. 120x180cm.', 1200.00, '/Content/images/produits/basket.jpg', 4, 5, 12, 2, 1, 1, 0),
('Coussin Tissé Traditionnel', 'Housse de coussin en tissage berbère traditionnel. Coloris naturels. 40x40cm.', 145.00, '/Content/images/produits/basket.jpg', 4, 5, 48, 10, 1, 0, 0);

-- ============================================
-- 5. IMAGES DES PRODUITS
-- ============================================

INSERT INTO ImagesProduits (ProduitId, UrlImage, EstPrincipale) VALUES
-- Savon Noir Beldi
(1, '/Content/images/produits/soap.jpg', 1),
(1, '/Content/images/produits/soap.jpg', 0),
(1, '/Content/images/produits/soap.jpg', 0),
-- Huile d'Argan
(2, '/Content/images/produits/argan-oil.jpg', 1),
(2, '/Content/images/produits/argan-oil.jpg', 0),
-- Savon d'Argan
(3, '/Content/images/produits/soap.jpg', 1),
-- Miel de Thym
(4, '/Content/images/produits/honey.jpg', 1),
(4, '/Content/images/produits/honey.jpg', 0);

-- ============================================
-- 6. VARIANTES DES PRODUITS
-- ============================================

INSERT INTO Variantes (ProduitId, Taille, Couleur, Stock, PrixSupplementaire, SKU, EstDisponible) VALUES
-- Savon Noir (formats)
(1, '250g', NULL, 120, 0.00, 'SN-250', 1),
(1, '500g', NULL, 80, 25.00, 'SN-500', 1),
-- Huile d'Argan (formats)
(2, '50ml', NULL, 90, 0.00, 'HA-50', 1),
(2, '100ml', NULL, 60, 80.00, 'HA-100', 1),
-- Miel (formats)
(4, '250g', NULL, 40, -30.00, 'MT-250', 1),
(4, '500g', NULL, 40, 0.00, 'MT-500', 1),
-- Tagine (tailles)
(8, '30cm', NULL, 25, 0.00, 'TAG-30', 1),
(8, '35cm', NULL, 20, 50.00, 'TAG-35', 1),
-- Panier (tailles)
(11, 'Petit', NULL, 15, -50.00, 'PAN-P', 1),
(11, 'Moyen', NULL, 12, 0.00, 'PAN-M', 1),
(11, 'Grand', NULL, 8, 80.00, 'PAN-G', 1);

-- ============================================
-- 7. MODES DE LIVRAISON (NOUVELLE STRUCTURE)
-- ============================================

INSERT INTO ModesLivraison (Nom, Description, Tarif, EstActif) VALUES
('Livraison Standard', 'Livraison à domicile dans toutes les villes du Maroc', 30.00, 1),
('Livraison Express', 'Livraison rapide dans les grandes villes', 60.00, 1);

-- ============================================
-- 8. ZONES DE LIVRAISON (NOUVELLE STRUCTURE)
-- ============================================
-- Avec délais min/max par mode et ville

INSERT INTO ZonesLivraison
(ZoneVille, Supplement, DelaiMinStandard, DelaiMaxStandard, DelaiMinExpress, DelaiMaxExpress, EstActif)
VALUES

-- AXE CASA – RABAT
('Casablanca', 0, 1, 2, 1, 2, 1),
('Mohammedia', 5, 1, 2, 1, 2, 1),
('Rabat', 5, 2, 3, 1, 2, 1),
('Salé', 5, 2, 3, 1, 2, 1),
('Témara', 5, 2, 3, 1, 2, 1),
('Skhirat', 5, 2, 3, 1, 2, 1),
('Kénitra', 5, 2, 3, 1, 2, 1),
('Sidi Kacem', 10, 2, 3, 1, 2, 1),
('Sidi Slimane', 10, 2, 3, 1, 2, 1),

-- NORD
('Tanger', 10, 2, 3, 1, 2, 1),
('Tétouan', 10, 2, 3, 1, 2, 1),
('Fnideq', 10, 2, 3, 1, 2, 1),
('M’diq', 10, 2, 3, 1, 2, 1),
('Martil', 10, 2, 3, 1, 2, 1),
('Larache', 10, 2, 3, 1, 2, 1),
('Asilah', 10, 2, 3, 1, 2, 1),
('Chefchaouen', 15, 2, 3, 1, 2, 1),
('Al Hoceima', 15, 2, 3, 1, 2, 1),

-- CENTRE
('Fès', 10, 2, 3, 1, 2, 1),
('Meknès', 10, 2, 3, 1, 2, 1),
('Ifrane', 15, 2, 3, 1, 2, 1),
('Azrou', 15, 2, 3, 1, 2, 1),
('Sefrou', 15, 2, 3, 1, 2, 1),
('Boulemane', 20, 3, 4, 2, 3, 1),

-- OUEST & ATLANTIQUE
('El Jadida', 10, 2, 3, 1, 2, 1),
('Azemmour', 10, 2, 3, 1, 2, 1),
('Safi', 15, 2, 3, 1, 2, 1),
('Essaouira', 15, 2, 3, 1, 2, 1),

-- SUD & CENTRE-SUD
('Marrakech', 10, 2, 3, 1, 2, 1),
('Chichaoua', 15, 2, 3, 1, 2, 1),
('El Kelaa des Sraghna', 15, 2, 3, 1, 2, 1),
('Beni Mellal', 15, 2, 3, 1, 2, 1),
('Khouribga', 15, 2, 3, 1, 2, 1),
('Settat', 10, 2, 3, 1, 2, 1),
('Berrechid', 10, 2, 3, 1, 2, 1),

-- ORIENTAL
('Oujda', 20, 3, 4, 2, 3, 1),
('Berkane', 20, 3, 4, 2, 3, 1),
('Nador', 20, 3, 4, 2, 3, 1),
('Taourirt', 20, 3, 4, 2, 3, 1),
('Jerada', 20, 3, 4, 2, 3, 1),

-- SUD / SAHARA
('Agadir', 15, 2, 3, 1, 2, 1),
('Taroudant', 20, 3, 4, 2, 3, 1),
('Tiznit', 20, 3, 4, 2, 3, 1),
('Ouarzazate', 25, 3, 5, 2, 4, 1),
('Zagora', 25, 3, 5, 2, 4, 1),
('Errachidia', 25, 3, 5, 2, 4, 1),
('Guelmim', 30, 4, 6, 3, 4, 1),
('Tan-Tan', 30, 4, 6, 3, 4, 1),
('Laâyoune', 35, 4, 6, 3, 4, 1),
('Boujdour', 35, 4, 6, 3, 4, 1),
('Smara', 35, 4, 6, 3, 4, 1),
('Dakhla', 40, 5, 7, 3, 5, 1),

-- FALLBACK
('Autres villes', 45, 5, 7, 3, 5, 1);


-- ============================================
-- 9. PANIERS ET ITEMS
-- ============================================

-- Panier Client 1 (Ahmed)
INSERT INTO Paniers (ClientId) VALUES (1);

INSERT INTO PanierItems (PanierId, ProduitId, VarianteId, Quantite, PrixUnitaire) VALUES
(1, 2, 2, 1, 180.00),  -- Huile d'Argan 100ml
(1, 4, NULL, 2, 120.00),  -- Miel de Thym x2
(1, 1, 1, 1, 65.00);  -- Savon Noir

-- Panier Client 2 (Fatima)
INSERT INTO Paniers (ClientId) VALUES (2);

INSERT INTO PanierItems (PanierId, ProduitId, VarianteId, Quantite, PrixUnitaire) VALUES
(2, 8, 6, 1, 350.00),  -- Tagine 30cm
(2, 6, NULL, 1, 85.00);  -- Amlou

-- ============================================
-- 10. COMMANDES
-- ============================================

INSERT INTO Commandes (NumeroCommande, ClientId, AdresseId, ModeLivraisonId, FraisLivraison, TotalHT, MontantTVA, TotalTTC, Statut) VALUES
('CMD-2025-00001', 3, 3, 1, 30.00, 545.00, 109.00, 684.00, 'Livrée'),
('CMD-2025-00002', 1, 1, 2, 60.00, 305.00, 61.00, 426.00, 'Expédiée'),
('CMD-2025-00003', 4, 4, 1, 30.00, 1200.00, 240.00, 1470.00, 'Préparation'),
('CMD-2025-00004', 2, 2, 2, 60.00, 280.00, 56.00, 396.00, 'Validée'),
('CMD-2025-00005', 5, 5, 1, 30.00, 415.00, 83.00, 528.00, 'Expédiée');

INSERT INTO CommandeItems (CommandeId, ProduitId, VarianteId, Quantite, PrixUnitaire, TotalLigne) VALUES
-- Commande 1
(1, 2, 2, 2, 180.00, 360.00),
(1, 1, 1, 1, 65.00, 65.00),
(1, 4, NULL, 1, 120.00, 120.00),
-- Commande 2
(2, 8, 5, 1, 350.00, 350.00),
-- Commande 3
(3, 12, NULL, 1, 1200.00, 1200.00),
-- Commande 4
(4, 9, NULL, 1, 280.00, 280.00),
-- Commande 5
(5, 6, NULL, 3, 85.00, 255.00),
(5, 3, NULL, 2, 45.00, 90.00),
(5, 5, NULL, 1, 95.00, 95.00);

-- ============================================
-- 11. SUIVI DES LIVRAISONS
-- ============================================

INSERT INTO LivraisonSuivi (CommandeId, Statut, Description, NumeroSuivi) VALUES
-- Commande 1 (Livrée)
(1, 'Validée', 'Commande validée et en cours de préparation', 'TRK2025001'),
(1, 'Préparation', 'Commande en cours de préparation dans nos locaux', 'TRK2025001'),
(1, 'Expédiée', 'Colis expédié vers Fès', 'TRK2025001'),
(1, 'Livrée', 'Commande livrée avec succès', 'TRK2025001'),
-- Commande 2 (Expédiée)
(2, 'Validée', 'Commande validée', 'TRK2025002'),
(2, 'Préparation', 'Produits emballés', 'TRK2025002'),
(2, 'Expédiée', 'En cours de livraison vers Casablanca', 'TRK2025002'),
-- Commande 3 (En préparation)
(3, 'Validée', 'Commande validée', 'TRK2025003'),
(3, 'Préparation', 'Commande en cours de préparation', 'TRK2025003'),
-- Commande 4 (Validée)
(4, 'Validée', 'Commande validée - En attente de retrait', NULL),
-- Commande 5 (Expédiée)
(5, 'Validée', 'Commande validée', 'TRK2025005'),
(5, 'Préparation', 'Emballage en cours', 'TRK2025005'),
(5, 'Expédiée', 'Expédié vers Tétouan', 'TRK2025005');

-- ============================================
-- 12. AVIS PRODUITS
-- ============================================

INSERT INTO AvisProduits (ClientId, ProduitId, Note, Commentaire) VALUES
(3, 2, 5, 'Excellente huile d''argan ! Texture parfaite et odeur agréable. Je l''utilise tous les jours pour mes cheveux.'),
(3, 1, 4, 'Très bon savon noir, efficace pour le gommage. Texture un peu épaisse mais résultat impeccable.'),
(1, 8, 5, 'Magnifique tagine, les décors sont superbes ! Très bonne qualité, je recommande vivement.'),
(4, 12, 5, 'Superbe tapis berbère authentique ! Les motifs sont magnifiques et la qualité exceptionnelle. Livraison soignée.'),
(2, 9, 4, 'Beau plat à couscous traditionnel, parfait pour les repas en famille. La céramique est de qualité.'),
(5, 6, 5, 'L''amlou est délicieux ! Goût authentique, parfait au petit-déjeuner avec du pain frais.'),
(5, 3, 4, 'Bon savon, hydrate bien la peau. Parfum d''argan naturel très agréable.'),
(1, 4, 5, 'Miel de thym excellent ! Goût intense et naturel, parfait pour les tisanes. Je rachèterai !'),
(3, 4, 5, 'Le meilleur miel que j''ai goûté ! Qualité exceptionnelle du Haut Atlas.'),
(2, 2, 5, 'Huile d''argan pure et de qualité supérieure. Très satisfaite de mon achat.'),
(2, 5, 5, 'Safran de qualité supérieure. Très satisfaite de mon achat.');

-- ============================================
-- VÉRIFICATION DES DONNÉES
-- ============================================

DECLARE @CountUtilisateurs INT;
DECLARE @CountClients INT;
DECLARE @CountCooperatives INT;
DECLARE @CountCategories INT;
DECLARE @CountProduits INT;
DECLARE @CountModesLivraison INT;
DECLARE @CountZonesLivraison INT;
DECLARE @CountCommandes INT;

SELECT @CountUtilisateurs = COUNT(*) FROM Utilisateurs;
SELECT @CountClients = COUNT(*) FROM Clients;
SELECT @CountCooperatives = COUNT(*) FROM Cooperatives;
SELECT @CountCategories = COUNT(*) FROM Categories;
SELECT @CountProduits = COUNT(*) FROM Produits;
SELECT @CountModesLivraison = COUNT(*) FROM ModesLivraison;
SELECT @CountZonesLivraison = COUNT(*) FROM ZonesLivraison;
SELECT @CountCommandes = COUNT(*) FROM Commandes;

PRINT '========================================';
PRINT 'CRÉATION DE LA BASE DE DONNÉES TERMINÉE';
PRINT '========================================';
PRINT '';
PRINT 'Résumé des données insérées :';
PRINT '  - Utilisateurs : ' + CAST(@CountUtilisateurs AS NVARCHAR(10));
PRINT '  - Clients : ' + CAST(@CountClients AS NVARCHAR(10));
PRINT '  - Coopératives : ' + CAST(@CountCooperatives AS NVARCHAR(10));
PRINT '  - Catégories : ' + CAST(@CountCategories AS NVARCHAR(10));
PRINT '  - Produits : ' + CAST(@CountProduits AS NVARCHAR(10));
PRINT '  - Modes de livraison : ' + CAST(@CountModesLivraison AS NVARCHAR(10));
PRINT '  - Zones de livraison : ' + CAST(@CountZonesLivraison AS NVARCHAR(10));
PRINT '  - Commandes : ' + CAST(@CountCommandes AS NVARCHAR(10));
PRINT '';
PRINT 'Vérification de la structure de livraison :';
SELECT 
    ModeLivraisonId,
    Nom,
    Tarif,
    EstActif  
FROM ModesLivraison
ORDER BY ModeLivraisonId;
PRINT '';
SELECT 
    ZoneLivraisonId,
    ZoneVille,
    Supplement,
    DelaiMinStandard,
    DelaiMaxStandard,
    DelaiMinExpress,
    DelaiMaxExpress,
    EstActif
FROM ZonesLivraison
ORDER BY ZoneVille;
PRINT '';
PRINT 'Base de données créée avec succès !';
GO

