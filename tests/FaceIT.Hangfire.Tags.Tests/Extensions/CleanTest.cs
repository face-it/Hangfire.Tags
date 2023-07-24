using Xunit;

namespace Hangfire.Tags.Tests.Extensions
{
    public class CleanTest
    {
        [Theory]
        [InlineData("Contract,: 3", "contract-3", Tags.Clean.Default)]
        [InlineData("AbcDef._-,: 456:-22", "abcdef-456-22", Tags.Clean.Default)]

        [InlineData("Contract,: 3", "contract: 3", Tags.Clean.Lowercase)]
        [InlineData("AbcDef._-,: 456:-22", "abcdef._-: 456:-22", Tags.Clean.Lowercase)]

        [InlineData("Contract,: 3", "Contract-3", Tags.Clean.Punctuation)]
        [InlineData("AbcDef._-,: 456:-22", "AbcDef-456-22", Tags.Clean.Punctuation)]
        
        [InlineData("Contract,: 3", "Contract: 3", Tags.Clean.None)]
        [InlineData("AbcDef._-,: 456:-22", "AbcDef._-: 456:-22", Tags.Clean.None)]
        public void Clean(string tag, string expected, Clean clean)
        {
            var result = tag.Clean(clean);
            Assert.Equal(expected, result);
        }
   }
}