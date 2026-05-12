using jsConnect.Vanilla;
using Xunit;

namespace jsConnect.Tests.Vanilla;

public class UsernameSanitizerTests
{
    [Theory]
    [InlineData("Alice Smith", "AliceSmith")]
    [InlineData("Alice  Smith", "AliceSmith")]
    [InlineData("\tAlice\nSmith", "AliceSmith")]
    [InlineData("AliceSmith", "AliceSmith")]
    [InlineData(" ", "")]
    public void RemoveWhitespace_strips_all_whitespace(string input, string expected)
    {
        Assert.Equal(expected, UsernameSanitizer.RemoveWhitespace(input));
    }

    [Theory]
    [InlineData("Švihlík", "Svihlik")]
    [InlineData("naïve", "naive")]
    [InlineData("Zoë", "Zoe")]
    [InlineData("ASCII", "ASCII")]
    public void RemoveAccents_strips_diacritics(string input, string expected)
    {
        Assert.Equal(expected, UsernameSanitizer.RemoveAccents(input));
    }

    [Fact]
    public void Sanitize_composes_both_steps_by_default()
    {
        Assert.Equal("PetrSvihlik", UsernameSanitizer.Sanitize("Petr Švihlík"));
    }

    [Fact]
    public void Sanitize_can_keep_whitespace()
    {
        Assert.Equal("Petr Svihlik", UsernameSanitizer.Sanitize("Petr Švihlík", allowWhitespace: true));
    }

    [Fact]
    public void Sanitize_can_keep_accents()
    {
        Assert.Equal("PetrŠvihlík", UsernameSanitizer.Sanitize("Petr Švihlík", allowAccents: true));
    }

    [Fact]
    public void Sanitize_is_a_no_op_when_both_flags_are_on()
    {
        Assert.Equal("Petr Švihlík", UsernameSanitizer.Sanitize("Petr Švihlík", allowWhitespace: true, allowAccents: true));
    }

    [Fact]
    public void RemoveWhitespace_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => UsernameSanitizer.RemoveWhitespace(null!));
    }

    [Fact]
    public void RemoveAccents_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => UsernameSanitizer.RemoveAccents(null!));
    }
}
