#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "quirc.h"
#include "zbar.h"

/* Input: id, dimension, hex row-major MSB-first modules. No C# code is used.
 * quirc decodes the module grid; ZBar scans a 4x raster with four quiet modules.
 * Output fields retain status and length-delimited payload bytes as hex. */
static void hex(const unsigned char *data, int length)
{
    for (int i = 0; i < length; i++) printf("%02x", data[i]);
}

int main(void)
{
    char id[256], packed[8000];
    int size;
    while (scanf("%255s %d %7999s", id, &size, packed) == 3) {
        if (size < 21 || size > 177 || strlen(packed) != (size * size + 7) / 8 * 2) return 2;
        struct quirc_code code = {0};
        struct quirc_data data;
        code.size = size;
        int pixels = (size + 8) * 4;
        unsigned char *raster = malloc(pixels * pixels);
        if (!raster) return 3;
        memset(raster, 255, pixels * pixels);
        for (int i = 0; i < size * size; i++) {
            unsigned int byte;
            if (sscanf(packed + i / 8 * 2, "%2x", &byte) != 1) return 4;
            if (!(byte & (128 >> (i % 8)))) continue;
            code.cell_bitmap[i / 8] |= 1 << (i % 8);
            for (int dy = 0; dy < 4; dy++)
                for (int dx = 0; dx < 4; dx++)
                    raster[((i / size + 4) * 4 + dy) * pixels + (i % size + 4) * 4 + dx] = 0;
        }
        int error = quirc_decode(&code, &data);
        printf("%s\t%d\t", id, error);
        if (!error) hex(data.payload, data.payload_len);
        printf("\t%d\t%d\t%d\t", error ? -1 : data.version, error ? -1 : data.ecc_level, error ? -1 : data.mask);
        zbar_image_scanner_t *scanner = zbar_image_scanner_create();
        zbar_image_scanner_set_config(scanner, ZBAR_NONE, ZBAR_CFG_ENABLE, 0);
        zbar_image_scanner_set_config(scanner, ZBAR_QRCODE, ZBAR_CFG_ENABLE, 1);
        zbar_image_scanner_set_config(scanner, ZBAR_QRCODE, ZBAR_CFG_BINARY, 1);
        zbar_image_t *image = zbar_image_create();
        zbar_image_set_format(image, zbar_fourcc('Y', '8', '0', '0'));
        zbar_image_set_size(image, pixels, pixels);
        zbar_image_set_data(image, raster, pixels * pixels, NULL);
        int count = zbar_scan_image(scanner, image);
        printf("%d\t", count);
        const zbar_symbol_t *symbol = zbar_image_first_symbol(image);
        if (count == 1 && symbol) hex((const unsigned char *)zbar_symbol_get_data(symbol), zbar_symbol_get_data_length(symbol));
        putchar('\n');
        fflush(stdout);
        zbar_image_destroy(image);
        zbar_image_scanner_destroy(scanner);
        free(raster);
    }
    return 0;
}
