using argon2;

// This is a fixed, synthetic example password. The library generates a fresh salt.
const string password = "example-only: correct horse battery staple";
var options = new Argon2PasswordHasherOptions();
var hasher = new Argon2PasswordHasher(options);
Console.WriteLine("Argon2 password hashing with the library defaults");
Console.WriteLine(
    $"Memory: {options.Parameters.MemorySizeKiB} KiB; " +
    $"passes: {options.Parameters.Iterations}; lanes: {options.Parameters.DegreeOfParallelism}");
string encoded = hasher.HashPasswordText(password);
Console.WriteLine($"Stored PHC string (changes on each run):\n{encoded}");

// Verification reads the salt and hashing parameters from the stored PHC string.
var correct = hasher.VerifyPasswordText(password, encoded);
var incorrect = hasher.VerifyPasswordText("example-only: wrong password", encoded);
Console.WriteLine($"Correct password: {correct.Status}");
Console.WriteLine($"Wrong password:   {incorrect.Status}");
Console.WriteLine($"Needs rehash with current options: {hasher.NeedsRehash(encoded)}");
if (correct.Status != Argon2VerificationStatus.Match || incorrect.Status != Argon2VerificationStatus.Mismatch)
{
    throw new InvalidOperationException("Password verification returned an unexpected result.");
}
