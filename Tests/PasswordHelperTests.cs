using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using E_Commerce_Cooperatives.Models;

namespace E_Commerce_Cooperatives.Tests
{
    [TestClass]
    public class PasswordHelperTests
    {
        [TestMethod]
        public void HashPassword_ShouldReturnValidHash()
        {
            // Arrange
            string password = "TestPassword123";

            // Act
            string hash = PasswordHelper.HashPassword(password);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(hash));
            Assert.AreNotEqual(password, hash);
        }

        [TestMethod]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            // Arrange
            string password = "TestPassword123";
            string hash = PasswordHelper.HashPassword(password);

            // Act
            bool isValid = PasswordHelper.VerifyPassword(password, hash);

            // Assert
            Assert.IsTrue(isValid);
        }
    }
}
