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

        public void Dispose()
        {
            // Nothing to dispose in this implementation
        }
    }
}


