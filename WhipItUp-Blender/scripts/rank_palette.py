"""The 7 rank colour sets shared by every WhipItUp item (D-005, D-014, D-021, D-023).

PALETTE: rank -> (crop texture, main leather, fittings, accent), sampled from the RidingCropMod albedos.
contrast_colour(rank): the second colour of tooling / braids (highest WCAG contrast with the leather, or the user's override).
"""

# Rank palette sampled from the RidingCropMod albedos (median of the shaft / fitting block / collar areas).
# rank: (crop texture, main leather, fittings, accent)
PALETTE = {
    1: ("crop_Albedo",    (15, 15, 17),    (224, 182, 106), (16, 16, 18)),
    2: ("crop_Albedo_V1", (195, 16, 23),   (14, 14, 16),    (222, 222, 222)),
    3: ("crop_Albedo_V3", (15, 15, 17),    (185, 15, 22),   (199, 16, 24)),
    4: ("crop_Albedo_V4", (219, 219, 219), (14, 14, 16),    (20, 20, 22)),
    5: ("crop_Albedo_V6", (136, 56, 6),    (14, 14, 16),    (20, 20, 19)),
    6: ("crop_Albedo_V2", (14, 68, 27),    (14, 14, 16),    (17, 20, 22)),
    7: ("crop_Albedo_V5", (141, 136, 127), (110, 66, 35),   (108, 65, 34)),
}
# Per-rank medallion strand colour override (user: V6 gets a muddy, Asksvin-like yellow instead of black on green)
INLAY_OVERRIDE = {6: (170, 150, 70)}
def luminance(c):
    r, g, b = [(v / 255.0) ** 2.2 for v in c]
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast_colour(main, fit, acc):
    """Medallion strand colour (user 2026-10-21, 'auto: highest contrast'): the fitting or accent colour whose
    WCAG contrast ratio against the rank's main leather is the highest."""
    def ratio(a, b):
        la, lb = luminance(a) + 0.05, luminance(b) + 0.05
        return max(la, lb) / min(la, lb)
    return fit if ratio(fit, main) >= ratio(acc, main) else acc




def second_colour(rank):
    """The rank's contrast colour (medallion strands, braided falls, grip braid)."""
    crop, main, fit, acc = PALETTE[rank]
    return INLAY_OVERRIDE.get(rank) or contrast_colour(main, fit, acc)
