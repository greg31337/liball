import com.google.zxing.EncodeHintType;
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel;
import com.google.zxing.qrcode.encoder.Encoder;
import com.google.zxing.qrcode.encoder.QRCode;
import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Base64;
import java.util.EnumMap;
import java.util.HexFormat;

// Test-side adapter: input is id, version, ECC, mask, mode, base64 UTF-8 text.
public class ZxingMatrixOracle {
    public static void main(String[] args) throws Exception {
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8));
        String line;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split("\t");
            int version = Integer.parseInt(fields[1]);
            int mask = Integer.parseInt(fields[3]);
            EnumMap<EncodeHintType, Object> hints = new EnumMap<>(EncodeHintType.class);
            hints.put(EncodeHintType.QR_VERSION, version);
            hints.put(EncodeHintType.QR_MASK_PATTERN, mask);
            if (fields[4].equals("Kanji")) hints.put(EncodeHintType.CHARACTER_SET, "Shift_JIS");
            String text = new String(Base64.getDecoder().decode(fields[5]), StandardCharsets.UTF_8);
            QRCode code = Encoder.encode(text, ErrorCorrectionLevel.valueOf(fields[2]), hints);
            if (code.getVersion().getVersionNumber() != version || code.getMaskPattern() != mask
                || !code.getMode().name().equals(fields[4].toUpperCase())) {
                throw new IllegalStateException("Oracle changed the requested configuration: " + fields[0]);
            }
            StringBuilder rows = new StringBuilder();
            for (int y = 0; y < code.getMatrix().getHeight(); y++) {
                for (int x = 0; x < code.getMatrix().getWidth(); x++) {
                    rows.append(code.getMatrix().get(x, y) == 1 ? '1' : '0');
                }
                rows.append('\n');
            }
            byte[] hash = MessageDigest.getInstance("SHA-256").digest(rows.toString().getBytes(StandardCharsets.US_ASCII));
            System.out.println(fields[0] + "\t" + HexFormat.of().formatHex(hash));
        }
    }
}
