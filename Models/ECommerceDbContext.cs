using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Configuration;

namespace E_Commerce_Cooperatives.Models
{
    public class ECommerceDbContext : IDisposable
    {
        private string connectionString;

        public ECommerceDbContext()
        {
            var connection = ConfigurationManager.ConnectionStrings["ECommerceConnection"];
            if (connection != null)
            {
                connectionString = connection.ConnectionString;
            }
            else
            {
                // Valeur par défaut si la chaîne de connexion n'est pas trouvée
                connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ecommerce;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            }
        }

        public List<Categorie> GetCategories()
        {
            var categories = new List<Categorie>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT CategorieId, Nom, Description, ImageUrl, EstActive, DateCreation FROM Categories WHERE EstActive = 1";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Categorie
                            {
                                CategorieId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                                EstActive = reader.GetBoolean(4),
                                DateCreation = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return categories;
        }

        public List<Cooperative> GetCooperatives()
        {
            var cooperatives = new List<Cooperative>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT CooperativeId, Nom, Description, Adresse, Ville, Telephone, Logo, EstActive, DateCreation FROM Cooperatives WHERE EstActive = 1 ORDER BY Nom";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cooperatives.Add(new Cooperative
                            {
                                CooperativeId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Adresse = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Ville = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Telephone = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Logo = reader.IsDBNull(6) ? null : reader.GetString(6),
                                EstActive = reader.GetBoolean(7),
                                DateCreation = reader.GetDateTime(8)
                            });
                        }
                    }
                }
            }
            return cooperatives;
        }

        public Dictionary<int, int> GetProductCountsByCooperative()
        {
            var counts = new Dictionary<int, int>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT CooperativeId, COUNT(*) as ProductCount 
                    FROM Produits 
                    WHERE EstDisponible = 1 AND CooperativeId IS NOT NULL
                    GROUP BY CooperativeId";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var cooperativeId = reader.GetInt32(0);
                            var count = reader.GetInt32(1);
                            counts[cooperativeId] = count;
                        }
                    }
                }
            }
            return counts;
        }

        public decimal GetMaxPrice()
        {
            decimal maxPrice = 1500; // Default fallback
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT MAX(Prix) FROM Produits WHERE EstDisponible = 1";
                using (var command = new SqlCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        maxPrice = (decimal)result;
                    }
                }
            }
            // Round up to nearest 100
            return Math.Ceiling(maxPrice / 100) * 100;
        }

        public decimal GetMinPrice()
        {
            decimal minPrice = 0; // Default fallback
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT MIN(Prix) FROM Produits WHERE EstDisponible = 1";
                using (var command = new SqlCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        minPrice = (decimal)result;
                    }
                }
            }
            // Round down to nearest 10 (or keep exact floor)
            return Math.Floor(minPrice / 10) * 10;
        }

        public List<Produit> GetProduits(bool? estEnVedette = null, bool? estNouveau = null, int? categorieId = null, int page = 1, int pageSize = 9, string search = null, string sortOrder = "popular", decimal? minPrice = null, decimal? maxPrice = null, List<int> cooperativeIds = null, bool inStockOnly = false, int? minRating = null)
        {
            var produits = new List<Produit>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT p.ProduitId, p.Nom, p.Description, p.Prix, p.ImageUrl, p.CategorieId, p.CooperativeId, 
                           p.StockTotal, p.SeuilAlerte, p.EstDisponible, p.EstEnVedette, p.EstNouveau,
                           p.DateCreation, p.DateModification,
                           c.Nom as CategorieNom, c.Description as CategorieDescription,
                           coop.Nom as CooperativeNom
                    FROM Produits p
                    LEFT JOIN Categories c ON p.CategorieId = c.CategorieId
                    LEFT JOIN Cooperatives coop ON p.CooperativeId = coop.CooperativeId
                    WHERE p.EstDisponible = 1";
                
                if (estEnVedette.HasValue)
                    query += " AND p.EstEnVedette = @EstEnVedette";
                if (estNouveau.HasValue)
                    query += " AND p.EstNouveau = @EstNouveau";
                if (categorieId.HasValue)
                    query += " AND p.CategorieId = @CategorieId";
                if (!string.IsNullOrEmpty(search))
                    query += " AND (p.Nom LIKE @Search OR p.Description LIKE @Search)";

                if (minPrice.HasValue)
                    query += " AND p.Prix >= @MinPrice";
                if (maxPrice.HasValue)
                    query += " AND p.Prix <= @MaxPrice";
                
                if (cooperativeIds != null && cooperativeIds.Any())
                {
                    var coopIds = string.Join(",", cooperativeIds);
                    query += $" AND p.CooperativeId IN ({coopIds})";
                }

                if (inStockOnly)
                    query += " AND p.StockTotal > 0";

                if (minRating.HasValue)
                {
                    query += @" AND (SELECT AVG(CAST(Note AS FLOAT)) 
                                     FROM AvisProduits 
                                     WHERE ProduitId = p.ProduitId) >= @MinRating";
                }

                if (!string.IsNullOrEmpty(search))
                    query += " AND (p.Nom LIKE @Search OR p.Description LIKE @Search)";

                // Sorting Logic
                switch (sortOrder)
                {
                    case "newest":
                        query += " ORDER BY p.EstNouveau DESC, p.DateCreation DESC";
                        break;
                    case "price-asc":
                        query += " ORDER BY p.Prix ASC";
                        break;
                    case "price-desc":
                        query += " ORDER BY p.Prix DESC";
                        break;
                    // Note: Rating and Popularity require joins/subqueries for accurate sorting if not pre-calculated on Product table.
                    // Assuming for now simple sorting or fallback. 
                    // To do it properly with SQL for computed fields (Avg Rating, Count Reviews), we need to join Views or compute it.
                    // Given the query structure, let's try to join efficiently or use subqueries for ordering if strictly required.
                    // For simplicity and performance, usually these are cached fields, but let's check if we can join.
                    // The current query does LEFT JOINs but doesn't aggregate.
                    // LET'S USE A COMMON TABLE EXPRESSION (CTE) OR SUBQUERY APPROACH IF COMPLEX, 
                    // OR JUST SORT BY ID/DATE IF COMPLEXITY IS TOO HIGH FOR NOW WITHOUT SCHEMA CHANGES.
                    // HOWEVER, user asked for "Popularity". Usually means "Most ordered" or "Most viewed" or "Most Reviews".
                    // Let's assume "Most Reviews" for Popularity based on current schema (AvisProduits).
                    
                    case "rating":
                        // Complex sort: requires joining with avg rating. 
                        // Simplified: Do it in memory? No, pagination breaks.
                        // Correct way: Add subquery for sorting.
                         query += " ORDER BY (SELECT AVG(CAST(Note AS FLOAT)) FROM AvisProduits WHERE ProduitId = p.ProduitId) DESC";
                        break;
                    case "popular":
                    default:
                         // Popularity = Number of reviews?
                         query += " ORDER BY (SELECT COUNT(*) FROM AvisProduits WHERE ProduitId = p.ProduitId) DESC";
                        break;
                }
                
                // Secondary sort for stable pagination
                query += ", p.ProduitId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    if (!string.IsNullOrEmpty(search))
                        command.Parameters.AddWithValue("@Search", "%" + search + "%");
                    if (minPrice.HasValue)
                        command.Parameters.AddWithValue("@MinPrice", minPrice.Value);
                    if (maxPrice.HasValue)
                        command.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
                    
                    if (minRating.HasValue)
                        command.Parameters.AddWithValue("@MinRating", minRating.Value);

                    if (estEnVedette.HasValue)
                        command.Parameters.AddWithValue("@EstEnVedette", estEnVedette.Value);
                    if (estNouveau.HasValue)
                        command.Parameters.AddWithValue("@EstNouveau", estNouveau.Value);
                    if (categorieId.HasValue)
                        command.Parameters.AddWithValue("@CategorieId", categorieId.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var produit = new Produit
                            {
                                ProduitId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Prix = reader.GetDecimal(3),
                                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                                CategorieId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                CooperativeId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                StockTotal = reader.GetInt32(7),
                                SeuilAlerte = reader.GetInt32(8),
                                EstDisponible = reader.GetBoolean(9),
                                EstEnVedette = reader.GetBoolean(10),
                                EstNouveau = reader.GetBoolean(11),
                                DateCreation = reader.GetDateTime(12),
                                DateModification = reader.IsDBNull(13) ? (DateTime?)null : reader.GetDateTime(13),
                            };

                            if (!reader.IsDBNull(14))
                            {
                                produit.Categorie = new Categorie
                                {
                                    Nom = reader.GetString(14),
                                    Description = reader.IsDBNull(15) ? null : reader.GetString(15)
                                };
                            }

                            if (!reader.IsDBNull(16))
                            {
                                produit.Cooperative = new Cooperative
                                {
                                    Nom = reader.GetString(16)
                                };
                            }

                            produits.Add(produit);
                        }
                    }
                }
            }

            // Charger les images et avis pour chaque produit
            foreach (var produit in produits)
            {
                produit.Images = GetImagesProduit(produit.ProduitId);
                var avis = GetAvisProduit(produit.ProduitId);
                if (avis.Any())
                {
                    produit.NoteMoyenne = (decimal)avis.Average(a => a.Note);
                    produit.NombreAvis = avis.Count;
                }
            }

            return produits;
        }

        public int GetProduitsCount(bool? estEnVedette = null, bool? estNouveau = null, int? categorieId = null, string search = null, decimal? minPrice = null, decimal? maxPrice = null, List<int> cooperativeIds = null, bool inStockOnly = false, int? minRating = null)
        {
            int count = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(*) FROM Produits p WHERE p.EstDisponible = 1";

                if (estEnVedette.HasValue)
                    query += " AND p.EstEnVedette = @EstEnVedette";
                if (estNouveau.HasValue)
                    query += " AND p.EstNouveau = @EstNouveau";
                if (categorieId.HasValue)
                    query += " AND p.CategorieId = @CategorieId";
                if (!string.IsNullOrEmpty(search))
                    query += " AND (p.Nom LIKE @Search OR p.Description LIKE @Search)";

                if (minPrice.HasValue)
                    query += " AND p.Prix >= @MinPrice";
                if (maxPrice.HasValue)
                    query += " AND p.Prix <= @MaxPrice";
                
                if (cooperativeIds != null && cooperativeIds.Any())
                {
                    var coopIds = string.Join(",", cooperativeIds);
                    query += $" AND p.CooperativeId IN ({coopIds})";
                }

                if (inStockOnly)
                    query += " AND p.StockTotal > 0";
                
                if (minRating.HasValue)
                {
                    query += @" AND (SELECT AVG(CAST(Note AS FLOAT)) 
                                     FROM AvisProduits 
                                     WHERE ProduitId = p.ProduitId) >= @MinRating";
                }

                using (var command = new SqlCommand(query, connection))
                {
                    if (estEnVedette.HasValue)
                        command.Parameters.AddWithValue("@EstEnVedette", estEnVedette.Value);
                    if (estNouveau.HasValue)
                        command.Parameters.AddWithValue("@EstNouveau", estNouveau.Value);
                    if (categorieId.HasValue)
                        command.Parameters.AddWithValue("@CategorieId", categorieId.Value);
                    if (!string.IsNullOrEmpty(search))
                        command.Parameters.AddWithValue("@Search", "%" + search + "%");
                    if (minPrice.HasValue)
                        command.Parameters.AddWithValue("@MinPrice", minPrice.Value);
                    if (maxPrice.HasValue)
                        command.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
                    
                    if (minRating.HasValue)
                        command.Parameters.AddWithValue("@MinRating", minRating.Value);

                    count = (int)command.ExecuteScalar();
                }
            }
            return count;
        }

        public List<dynamic> GetSearchSuggestions(string term, int limit = 5)
        {
            var suggestions = new List<dynamic>();
            if (string.IsNullOrWhiteSpace(term))
                return suggestions;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT TOP (@Limit) p.ProduitId, p.Nom, p.Prix, p.ImageUrl 
                    FROM Produits p
                    WHERE p.EstDisponible = 1 AND p.Nom LIKE @Term
                    ORDER BY p.Nom";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Limit", limit);
                    command.Parameters.AddWithValue("@Term", "%" + term + "%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suggestions.Add(new
                            {
                                ProduitId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Prix = reader.GetDecimal(2),
                                ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return suggestions;
        }

        private List<ImageProduit> GetImagesProduit(int produitId)
        {
            var images = new List<ImageProduit>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT ImageId, UrlImage, EstPrincipale FROM ImagesProduits WHERE ProduitId = @ProduitId ORDER BY EstPrincipale DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProduitId", produitId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            images.Add(new ImageProduit
                            {
                                ImageId = reader.GetInt32(0),
                                UrlImage = reader.GetString(1),
                                EstPrincipale = reader.GetBoolean(2)
                            });
                        }
                    }
                }
            }
            return images;
        }

        private List<AvisProduit> GetAvisProduit(int produitId)
        {
            var avis = new List<AvisProduit>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"SELECT a.AvisId, a.ClientId, a.Note, a.Commentaire, a.DateAvis, 
                                    c.Nom + ' ' + c.Prenom as ClientNom
                             FROM AvisProduits a
                             LEFT JOIN Clients c ON a.ClientId = c.ClientId
                             WHERE a.ProduitId = @ProduitId
                             ORDER BY a.DateAvis DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProduitId", produitId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            avis.Add(new AvisProduit
                            {
                                AvisId = reader.GetInt32(0),
                                ClientId = reader.GetInt32(1),
                                Note = reader.GetInt32(2),
                                Commentaire = reader.IsDBNull(3) ? null : reader.GetString(3),
                                DateAvis = reader.GetDateTime(4),
                                ClientNom = reader.IsDBNull(5) ? "Client anonyme" : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return avis;
        }

        private List<Variante> GetVariantesProduit(int produitId)
        {
            var variantes = new List<Variante>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"SELECT VarianteId, Taille, Couleur, Stock, PrixSupplementaire, SKU, EstDisponible, DateCreation
                             FROM Variantes 
                             WHERE ProduitId = @ProduitId AND EstDisponible = 1
                             ORDER BY Taille, Couleur";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProduitId", produitId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            variantes.Add(new Variante
                            {
                                VarianteId = reader.GetInt32(0),
                                Taille = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Couleur = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Stock = reader.GetInt32(3),
                                PrixSupplementaire = reader.GetDecimal(4),
                                SKU = reader.IsDBNull(5) ? null : reader.GetString(5),
                                EstDisponible = reader.GetBoolean(6),
                                DateCreation = reader.GetDateTime(7)
                            });
                        }
                    }
                }
            }
            return variantes;
        }

        public Produit GetProduitDetails(int produitId)
        {
            Produit produit = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT p.ProduitId, p.Nom, p.Description, p.Prix, p.ImageUrl, p.CategorieId, p.CooperativeId, 
                           p.StockTotal, p.SeuilAlerte, p.EstDisponible, p.EstEnVedette, p.EstNouveau,
                           p.DateCreation, p.DateModification,
                           c.CategorieId, c.Nom as CategorieNom, c.Description as CategorieDescription,
                           coop.CooperativeId, coop.Nom as CooperativeNom, coop.Description as CooperativeDescription,
                           coop.Adresse as CooperativeAdresse, coop.Ville as CooperativeVille, coop.Telephone as CooperativeTelephone
                    FROM Produits p
                    LEFT JOIN Categories c ON p.CategorieId = c.CategorieId
                    LEFT JOIN Cooperatives coop ON p.CooperativeId = coop.CooperativeId
                    WHERE p.ProduitId = @ProduitId AND p.EstDisponible = 1";
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProduitId", produitId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            produit = new Produit
                            {
                                ProduitId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Prix = reader.GetDecimal(3),
                                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                                CategorieId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                CooperativeId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                StockTotal = reader.GetInt32(7),
                                SeuilAlerte = reader.GetInt32(8),
                                EstDisponible = reader.GetBoolean(9),
                                EstEnVedette = reader.GetBoolean(10),
                                EstNouveau = reader.GetBoolean(11),
                                DateCreation = reader.GetDateTime(12),
                                DateModification = reader.IsDBNull(13) ? (DateTime?)null : reader.GetDateTime(13)
                            };

                            if (!reader.IsDBNull(14))
                            {
                                produit.Categorie = new Categorie
                                {
                                    CategorieId = reader.GetInt32(14),
                                    Nom = reader.GetString(15),
                                    Description = reader.IsDBNull(16) ? null : reader.GetString(16)
                                };
                            }

                            if (!reader.IsDBNull(17))
                            {
                                produit.Cooperative = new Cooperative
                                {
                                    CooperativeId = reader.GetInt32(17),
                                    Nom = reader.GetString(18),
                                    Description = reader.IsDBNull(19) ? null : reader.GetString(19),
                                    Adresse = reader.IsDBNull(20) ? null : reader.GetString(20),
                                    Ville = reader.IsDBNull(21) ? null : reader.GetString(21),
                                    Telephone = reader.IsDBNull(22) ? null : reader.GetString(22)
                                };
                            }
                        }
                    }
                }
            }

            if (produit != null)
            {
                // Charger les images, variantes et avis
                produit.Images = GetImagesProduit(produit.ProduitId);
                produit.Variantes = GetVariantesProduit(produit.ProduitId);
                var avis = GetAvisProduit(produit.ProduitId);
                if (avis.Any())
                {
                    produit.NoteMoyenne = (decimal)avis.Average(a => a.Note);
                    produit.NombreAvis = avis.Count;
                }
                produit.Avis = avis;
            }

            return produit;
        }

        // ============================================
        // GESTION DES COMMANDES
        // ============================================

        public List<Commande> GetCommandes(string searchTerm = null, string statutFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, decimal? montantMin = null, decimal? montantMax = null)
        {
            var commandes = new List<Commande>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT c.CommandeId, c.NumeroCommande, c.ClientId, c.AdresseId, c.ModeLivraisonId, 
                           c.DateCommande, c.FraisLivraison, c.TotalHT, c.MontantTVA, c.TotalTTC, 
                           c.Statut, c.Commentaire, c.DateAnnulation, c.RaisonAnnulation,
                           cl.Nom, cl.Prenom, cl.Telephone, 
                           u.Email,
                           ml.Nom as ModeLivraisonNom
                    FROM Commandes c
                    INNER JOIN Clients cl ON c.ClientId = cl.ClientId
                    INNER JOIN Utilisateurs u ON cl.UtilisateurId = u.UtilisateurId
                    LEFT JOIN ModesLivraison ml ON c.ModeLivraisonId = ml.ModeLivraisonId
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(searchTerm))
                    query += " AND (c.NumeroCommande LIKE @SearchTerm OR cl.Nom LIKE @SearchTerm OR cl.Prenom LIKE @SearchTerm OR u.Email LIKE @SearchTerm)";
                
                if (!string.IsNullOrEmpty(statutFilter) && statutFilter != "all")
                    query += " AND c.Statut = @StatutFilter";
                
                if (dateFrom.HasValue)
                    query += " AND c.DateCommande >= @DateFrom";
                
                if (dateTo.HasValue)
                    query += " AND c.DateCommande <= @DateTo";
                
                if (montantMin.HasValue)
                    query += " AND c.TotalTTC >= @MontantMin";
                
                if (montantMax.HasValue)
                    query += " AND c.TotalTTC <= @MontantMax";

                query += " ORDER BY c.DateCommande DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(searchTerm))
                        command.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                    if (!string.IsNullOrEmpty(statutFilter) && statutFilter != "all")
                        command.Parameters.AddWithValue("@StatutFilter", statutFilter);
                    if (dateFrom.HasValue)
                        command.Parameters.AddWithValue("@DateFrom", dateFrom.Value);
                    if (dateTo.HasValue)
                        command.Parameters.AddWithValue("@DateTo", dateTo.Value);
                    if (montantMin.HasValue)
                        command.Parameters.AddWithValue("@MontantMin", montantMin.Value);
                    if (montantMax.HasValue)
                        command.Parameters.AddWithValue("@MontantMax", montantMax.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            commandes.Add(new Commande
                            {
                                CommandeId = reader.GetInt32(0),
                                NumeroCommande = reader.GetString(1),
                                ClientId = reader.GetInt32(2),
                                AdresseId = reader.GetInt32(3),
                                ModeLivraisonId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                DateCommande = reader.GetDateTime(5),
                                FraisLivraison = reader.GetDecimal(6),
                                TotalHT = reader.GetDecimal(7),
                                MontantTVA = reader.GetDecimal(8),
                                TotalTTC = reader.GetDecimal(9),
                                Statut = reader.IsDBNull(10) ? null : reader.GetString(10),
                                Commentaire = reader.IsDBNull(11) ? null : reader.GetString(11),
                                DateAnnulation = reader.IsDBNull(12) ? (DateTime?)null : reader.GetDateTime(12),
                                RaisonAnnulation = reader.IsDBNull(13) ? null : reader.GetString(13),
                                Client = new Client
                                {
                                    ClientId = reader.GetInt32(2),
                                    Nom = reader.GetString(14),
                                    Prenom = reader.GetString(15),
                                    Telephone = reader.IsDBNull(16) ? null : reader.GetString(16),
                                    Email = reader.GetString(17)
                                },
                                ModeLivraison = reader.IsDBNull(18) ? null : new ModeLivraison
                                {
                                    Nom = reader.GetString(18)
                                }
                            });
                        }
                    }
                }
            }

            // Charger les adresses et items pour chaque commande
            foreach (var commande in commandes)
            {
                commande.Adresse = GetAdresse(commande.AdresseId);
                commande.Items = GetCommandeItems(commande.CommandeId);
                commande.SuiviLivraison = GetLivraisonSuivi(commande.CommandeId);
            }

            return commandes;
        }

        public Commande GetCommandeDetails(int commandeId)
        {
            Commande commande = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT c.CommandeId, c.NumeroCommande, c.ClientId, c.AdresseId, c.ModeLivraisonId, 
                           c.DateCommande, c.FraisLivraison, c.TotalHT, c.MontantTVA, c.TotalTTC, 
                           c.Statut, c.Commentaire, c.DateAnnulation, c.RaisonAnnulation,
                           cl.Nom, cl.Prenom, cl.Telephone, 
                           u.Email,
                           ml.ModeLivraisonId, ml.Nom as ModeLivraisonNom, ml.Description as ModeLivraisonDesc, ml.Tarif
                    FROM Commandes c
                    INNER JOIN Clients cl ON c.ClientId = cl.ClientId
                    INNER JOIN Utilisateurs u ON cl.UtilisateurId = u.UtilisateurId
                    LEFT JOIN ModesLivraison ml ON c.ModeLivraisonId = ml.ModeLivraisonId
                    WHERE c.CommandeId = @CommandeId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CommandeId", commandeId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            commande = new Commande
                            {
                                CommandeId = reader.GetInt32(0),
                                NumeroCommande = reader.GetString(1),
                                ClientId = reader.GetInt32(2),
                                AdresseId = reader.GetInt32(3),
                                ModeLivraisonId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                DateCommande = reader.GetDateTime(5),
                                FraisLivraison = reader.GetDecimal(6),
                                TotalHT = reader.GetDecimal(7),
                                MontantTVA = reader.GetDecimal(8),
                                TotalTTC = reader.GetDecimal(9),
                                Statut = reader.IsDBNull(10) ? null : reader.GetString(10),
                                Commentaire = reader.IsDBNull(11) ? null : reader.GetString(11),
                                DateAnnulation = reader.IsDBNull(12) ? (DateTime?)null : reader.GetDateTime(12),
                                RaisonAnnulation = reader.IsDBNull(13) ? null : reader.GetString(13),
                                Client = new Client
                                {
                                    ClientId = reader.GetInt32(2),
                                    Nom = reader.GetString(14),
                                    Prenom = reader.GetString(15),
                                    Telephone = reader.IsDBNull(16) ? null : reader.GetString(16),
                                    Email = reader.GetString(17)
                                },
                                ModeLivraison = reader.IsDBNull(18) ? null : new ModeLivraison
                                {
                                    ModeLivraisonId = reader.GetInt32(18),
                                    Nom = reader.GetString(19),
                                    Description = reader.IsDBNull(20) ? null : reader.GetString(20),
                                    Tarif = reader.IsDBNull(21) ? 0 : reader.GetDecimal(21)
                                }
                            };
                        }
                    }
                }
            }

            if (commande != null)
            {
                commande.Adresse = GetAdresse(commande.AdresseId);
                commande.Items = GetCommandeItems(commande.CommandeId);
                commande.SuiviLivraison = GetLivraisonSuivi(commande.CommandeId);
            }

            return commande;
        }

        private Adresse GetAdresse(int adresseId)
        {
            Adresse adresse = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT AdresseId, ClientId, AdresseComplete, Ville, CodePostal, Pays, EstParDefaut, DateCreation FROM Adresses WHERE AdresseId = @AdresseId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AdresseId", adresseId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            adresse = new Adresse
                            {
                                AdresseId = reader.GetInt32(0),
                                ClientId = reader.GetInt32(1),
                                AdresseComplete = reader.GetString(2),
                                Ville = reader.GetString(3),
                                CodePostal = reader.GetString(4),
                                Pays = reader.IsDBNull(5) ? "Maroc" : reader.GetString(5),
                                EstParDefaut = reader.GetBoolean(6),
                                DateCreation = reader.GetDateTime(7)
                            };
                        }
                    }
                }
            }
            return adresse;
        }

        private List<CommandeItem> GetCommandeItems(int commandeId)
        {
            var items = new List<CommandeItem>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT ci.CommandeItemId, ci.CommandeId, ci.ProduitId, ci.VarianteId, ci.Quantite, ci.PrixUnitaire, ci.TotalLigne,
                           p.Nom as ProduitNom, p.ImageUrl as ProduitImage
                    FROM CommandeItems ci
                    INNER JOIN Produits p ON ci.ProduitId = p.ProduitId
                    WHERE ci.CommandeId = @CommandeId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CommandeId", commandeId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new CommandeItem
                            {
                                CommandeItemId = reader.GetInt32(0),
                                CommandeId = reader.GetInt32(1),
                                ProduitId = reader.GetInt32(2),
                                VarianteId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                Quantite = reader.GetInt32(4),
                                PrixUnitaire = reader.GetDecimal(5),
                                TotalLigne = reader.GetDecimal(6),
                                Produit = new Produit
                                {
                                    ProduitId = reader.GetInt32(2),
                                    Nom = reader.GetString(7),
                                    ImageUrl = reader.IsDBNull(8) ? null : reader.GetString(8)
                                }
                            });
                        }
                    }
                }
            }
            return items;
        }

        private List<LivraisonSuivi> GetLivraisonSuivi(int commandeId)
        {
            var suivi = new List<LivraisonSuivi>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT SuiviId, CommandeId, Statut, Description, NumeroSuivi, DateStatut FROM LivraisonSuivi WHERE CommandeId = @CommandeId ORDER BY DateStatut DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CommandeId", commandeId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suivi.Add(new LivraisonSuivi
                            {
                                SuiviId = reader.GetInt32(0),
                                CommandeId = reader.GetInt32(1),
                                Statut = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                NumeroSuivi = reader.IsDBNull(4) ? null : reader.GetString(4),
                                DateStatut = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return suivi;
        }

        public Dictionary<string, int> GetCommandeStats()
        {
            var stats = new Dictionary<string, int>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT 
                        COUNT(*) as Total,
                        SUM(CASE WHEN Statut = 'Validée' THEN 1 ELSE 0 END) as Validees,
                        SUM(CASE WHEN Statut = 'Préparation' THEN 1 ELSE 0 END) as Preparation,
                        SUM(CASE WHEN Statut = 'Expédiée' THEN 1 ELSE 0 END) as Expediees,
                        SUM(CASE WHEN Statut = 'Livrée' THEN 1 ELSE 0 END) as Livrees,
                        SUM(CASE WHEN Statut = 'Annulée' THEN 1 ELSE 0 END) as Annulees
                    FROM Commandes";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats["Total"] = reader.GetInt32(0);
                            stats["Validée"] = reader.GetInt32(1);
                            stats["Préparation"] = reader.GetInt32(2);
                            stats["Expédiée"] = reader.GetInt32(3);
                            stats["Livrée"] = reader.GetInt32(4);
                            stats["Annulée"] = reader.GetInt32(5);
                        }
                    }
                }
            }
            return stats;
        }

        public bool UpdateCommandeStatut(int commandeId, string nouveauStatut)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "UPDATE Commandes SET Statut = @Statut WHERE CommandeId = @CommandeId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Statut", nouveauStatut);
                    command.Parameters.AddWithValue("@CommandeId", commandeId);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool AnnulerCommande(int commandeId, string raisonAnnulation)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Vérifier d'abord si la commande existe
                    var checkQuery = "SELECT COUNT(*) FROM Commandes WHERE CommandeId = @CommandeId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@CommandeId", commandeId);
                        var exists = (int)checkCommand.ExecuteScalar() > 0;
                        if (!exists)
                        {
                            return false;
                        }
                    }
                    
                    // Mettre à jour la commande
                    var query = "UPDATE Commandes SET Statut = 'Annulée', DateAnnulation = GETDATE(), RaisonAnnulation = @Raison WHERE CommandeId = @CommandeId";
                    using (var command = new SqlCommand(query, connection))
                    {
                        // Limiter la longueur de la raison à 500 caractères (taille de la colonne)
                        var raison = raisonAnnulation ?? "";
                        if (raison.Length > 500)
                        {
                            raison = raison.Substring(0, 500);
                        }
                        
                        command.Parameters.AddWithValue("@Raison", raison);
                        command.Parameters.AddWithValue("@CommandeId", commandeId);
                        var rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Gérer les erreurs SQL spécifiques (comme les contraintes CHECK)
                throw new Exception("Erreur SQL : " + sqlEx.Message, sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de l'annulation : " + ex.Message, ex);
            }
        }

        // ============================================
        // GESTION DES MODES DE LIVRAISON
        // ============================================

        public List<ModeLivraison> GetModesLivraison()
        {
            var modes = new List<ModeLivraison>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT ModeLivraisonId, Nom, Description, Tarif, DelaiEstime, EstActif, DateCreation FROM ModesLivraison ORDER BY DateCreation DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            modes.Add(new ModeLivraison
                            {
                                ModeLivraisonId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Tarif = reader.GetDecimal(3),
                                DelaiEstime = reader.IsDBNull(4) ? null : reader.GetString(4),
                                EstActif = reader.GetBoolean(5),
                                DateCreation = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return modes;
        }

        public ModeLivraison GetModeLivraison(int id)
        {
            ModeLivraison mode = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT ModeLivraisonId, Nom, Description, Tarif, DelaiEstime, EstActif, DateCreation FROM ModesLivraison WHERE ModeLivraisonId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mode = new ModeLivraison
                            {
                                ModeLivraisonId = reader.GetInt32(0),
                                Nom = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Tarif = reader.GetDecimal(3),
                                DelaiEstime = reader.IsDBNull(4) ? null : reader.GetString(4),
                                EstActif = reader.GetBoolean(5),
                                DateCreation = reader.GetDateTime(6)
                            };
                        }
                    }
                }
            }
            return mode;
        }

        public bool CreateModeLivraison(ModeLivraison mode)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"INSERT INTO ModesLivraison (Nom, Description, Tarif, DelaiEstime, EstActif, DateCreation) 
                             VALUES (@Nom, @Description, @Tarif, @DelaiEstime, @EstActif, GETDATE())";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nom", mode.Nom);
                    command.Parameters.AddWithValue("@Description", (object)mode.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Tarif", mode.Tarif);
                    command.Parameters.AddWithValue("@DelaiEstime", (object)mode.DelaiEstime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EstActif", mode.EstActif);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateModeLivraison(ModeLivraison mode)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"UPDATE ModesLivraison 
                             SET Nom = @Nom, Description = @Description, Tarif = @Tarif, 
                                 DelaiEstime = @DelaiEstime, EstActif = @EstActif 
                             WHERE ModeLivraisonId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", mode.ModeLivraisonId);
                    command.Parameters.AddWithValue("@Nom", mode.Nom);
                    command.Parameters.AddWithValue("@Description", (object)mode.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Tarif", mode.Tarif);
                    command.Parameters.AddWithValue("@DelaiEstime", (object)mode.DelaiEstime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EstActif", mode.EstActif);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteModeLivraison(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Vérifier si le mode est utilisé dans des commandes
                var checkQuery = "SELECT COUNT(*) FROM Commandes WHERE ModeLivraisonId = @Id";
                using (var checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var count = (int)checkCommand.ExecuteScalar();
                    if (count > 0)
                    {
                        // Si utilisé, on désactive au lieu de supprimer
                        var updateQuery = "UPDATE ModesLivraison SET EstActif = 0 WHERE ModeLivraisonId = @Id";
                        using (var updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Id", id);
                            return updateCommand.ExecuteNonQuery() > 0;
                        }
                    }
                    else
                    {
                        // Si non utilisé, on peut supprimer
                        var deleteQuery = "DELETE FROM ModesLivraison WHERE ModeLivraisonId = @Id";
                        using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@Id", id);
                            return deleteCommand.ExecuteNonQuery() > 0;
                        }
                    }
                }
            }
        }

        public Dictionary<string, object> GetLivraisonStats()
        {
            var stats = new Dictionary<string, object>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Nombre de modes de livraison actifs
                var modesQuery = "SELECT COUNT(*) FROM ModesLivraison WHERE EstActif = 1";
                using (var command = new SqlCommand(modesQuery, connection))
                {
                    stats["ModesActifs"] = (int)command.ExecuteScalar();
                }

                // Nombre total de modes
                var totalModesQuery = "SELECT COUNT(*) FROM ModesLivraison";
                using (var command = new SqlCommand(totalModesQuery, connection))
                {
                    stats["TotalModes"] = (int)command.ExecuteScalar();
                }

                // Nombre de livraisons ce mois
                var livraisonsQuery = @"SELECT COUNT(*) FROM Commandes 
                                       WHERE MONTH(DateCommande) = MONTH(GETDATE()) 
                                       AND YEAR(DateCommande) = YEAR(GETDATE())
                                       AND Statut IN ('Expédiée', 'Livrée')";
                using (var command = new SqlCommand(livraisonsQuery, connection))
                {
                    stats["LivraisonsMois"] = (int)command.ExecuteScalar();
                }

                // Tarif moyen
                var tarifQuery = "SELECT AVG(Tarif) FROM ModesLivraison WHERE EstActif = 1";
                using (var command = new SqlCommand(tarifQuery, connection))
                {
                    var result = command.ExecuteScalar();
                    stats["TarifMoyen"] = result != DBNull.Value ? (decimal)result : 0;
                }

                // Nombre total de zones de livraison
                var zonesQuery = "SELECT COUNT(*) FROM ZonesLivraison";
                using (var command = new SqlCommand(zonesQuery, connection))
                {
                    stats["TotalZones"] = (int)command.ExecuteScalar();
                }
            }
            return stats;
        }

        // ============================================
        // ZONES DE LIVRAISON
        // ============================================

        public List<ZoneLivraison> GetZonesLivraison()
        {
            var zones = new List<ZoneLivraison>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT ZoneLivraisonId, ZoneVille, Supplement, DelaiEstime, EstActif, DateCreation FROM ZonesLivraison ORDER BY ZoneVille ASC";
                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        zones.Add(new ZoneLivraison
                        {
                            ZoneLivraisonId = reader.GetInt32(0),
                            ZoneVille = reader.GetString(1),
                            Supplement = reader.GetDecimal(2),
                            DelaiEstime = reader.GetString(3),
                            EstActif = reader.GetBoolean(4),
                            DateCreation = reader.GetDateTime(5)
                        });
                    }
                }
            }
            return zones;
        }

        public PagedResult<ZoneLivraison> GetZonesLivraisonPaged(int pageNumber, int pageSize)
        {
            var result = new PagedResult<ZoneLivraison>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = new List<ZoneLivraison>()
            };

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Compter le total
                var countQuery = "SELECT COUNT(*) FROM ZonesLivraison";
                using (var countCommand = new SqlCommand(countQuery, connection))
                {
                    result.TotalCount = (int)countCommand.ExecuteScalar();
                }

                // Récupérer les données paginées
                var query = @"SELECT ZoneLivraisonId, ZoneVille, Supplement, DelaiEstime, EstActif, DateCreation 
                             FROM ZonesLivraison 
                             ORDER BY ZoneVille ASC
                             OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new ZoneLivraison
                            {
                                ZoneLivraisonId = reader.GetInt32(0),
                                ZoneVille = reader.GetString(1),
                                Supplement = reader.GetDecimal(2),
                                DelaiEstime = reader.GetString(3),
                                EstActif = reader.GetBoolean(4),
                                DateCreation = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return result;
        }

        public ZoneLivraison GetZoneLivraison(int id)
        {
            ZoneLivraison zone = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT ZoneLivraisonId, ZoneVille, Supplement, DelaiEstime, EstActif, DateCreation FROM ZonesLivraison WHERE ZoneLivraisonId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            zone = new ZoneLivraison
                            {
                                ZoneLivraisonId = reader.GetInt32(0),
                                ZoneVille = reader.GetString(1),
                                Supplement = reader.GetDecimal(2),
                                DelaiEstime = reader.GetString(3),
                                EstActif = reader.GetBoolean(4),
                                DateCreation = reader.GetDateTime(5)
                            };
                        }
                    }
                }
            }
            return zone;
        }

        public bool CreateZoneLivraison(ZoneLivraison zone)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"INSERT INTO ZonesLivraison (ZoneVille, Supplement, DelaiEstime, EstActif, DateCreation)
                             VALUES (@ZoneVille, @Supplement, @DelaiEstime, @EstActif, GETDATE())";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ZoneVille", zone.ZoneVille);
                    command.Parameters.AddWithValue("@Supplement", zone.Supplement);
                    command.Parameters.AddWithValue("@DelaiEstime", zone.DelaiEstime);
                    command.Parameters.AddWithValue("@EstActif", zone.EstActif);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool UpdateZoneLivraison(ZoneLivraison zone)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"UPDATE ZonesLivraison
                             SET ZoneVille = @ZoneVille, Supplement = @Supplement, DelaiEstime = @DelaiEstime, EstActif = @EstActif
                             WHERE ZoneLivraisonId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", zone.ZoneLivraisonId);
                    command.Parameters.AddWithValue("@ZoneVille", zone.ZoneVille);
                    command.Parameters.AddWithValue("@Supplement", zone.Supplement);
                    command.Parameters.AddWithValue("@DelaiEstime", zone.DelaiEstime);
                    command.Parameters.AddWithValue("@EstActif", zone.EstActif);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteZoneLivraison(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Vérifier si la zone est utilisée dans des commandes (via adresses)
                // Pour l'instant, on supprime directement
                var deleteQuery = "DELETE FROM ZonesLivraison WHERE ZoneLivraisonId = @Id";
                using (var command = new SqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose in this implementation
        }
    }
}

