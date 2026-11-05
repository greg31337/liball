import com.google.zxing.DecodeHintType;
import com.google.zxing.common.BitMatrix;
import com.google.zxing.common.DecoderResult;
import com.google.zxing.qrcode.decoder.Decoder;
import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.EnumMap;

// Input: id, square dimension, base64 row-major MSB-first packed modules.
// Output: id, status, base64 UTF-8 text, base64 byte-mode payload, ECC level,
// structured-append sequence byte and parity (-1 when absent).
public class ZxingDecoderOracle {
    public static void main(String[] args) throws Exception {
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8));
        String line;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split("\t");
            int size = Integer.parseInt(fields[1]);
            byte[] packed = Base64.getDecoder().decode(fields[2]);
            BitMatrix matrix = new BitMatrix(size);
            for (int i = 0; i < size * size; i++)
                if ((packed[i / 8] & (128 >> (i % 8))) != 0) matrix.set(i % size, i / size);
            EnumMap<DecodeHintType, Object> hints = new EnumMap<>(DecodeHintType.class);
            hints.put(DecodeHintType.CHARACTER_SET, "ISO-8859-1");
            try {
                DecoderResult result = new Decoder().decode(matrix, hints);
                java.io.ByteArrayOutputStream bytes = new java.io.ByteArrayOutputStream();
                if (result.getByteSegments() != null)
                    for (byte[] segment : result.getByteSegments()) bytes.write(segment);
                System.out.println(fields[0] + "\tOK\t" + Base64.getEncoder().encodeToString(result.getText().getBytes(StandardCharsets.UTF_8))
                    + "\t" + Base64.getEncoder().encodeToString(bytes.toByteArray()) + "\t" + result.getECLevel()
                    + "\t" + result.getStructuredAppendSequenceNumber() + "\t" + result.getStructuredAppendParity());
            } catch (com.google.zxing.ReaderException error) {
                System.out.println(fields[0] + "\t" + error.getClass().getSimpleName());
            }
        }
    }
}
