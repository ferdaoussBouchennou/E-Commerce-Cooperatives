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

        public List<Produit> GetProduits(bool? estEnVedette = null, bool? estNouveau = null, int? categorieId = null)
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

                query += " ORDER BY p.DateCreation DESC";

                using (var command = new SqlCommand(query, connection))
                {
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
                var query = "SELECT Note, Commentaire FROM AvisProduits WHERE ProduitId = @ProduitId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProduitId", produitId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            avis.Add(new AvisProduit
                            {
                                Note = reader.GetInt32(0),
                                Commentaire = reader.IsDBNull(1) ? null : reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return avis;
        }

        public void Dispose()
        {
            // Nothing to dispose in this implementation
        }
    }
}


