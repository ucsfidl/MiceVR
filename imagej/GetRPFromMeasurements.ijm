macro "GetRP [r]" {
	area = getResult("Area", 0);
	diameter = sqrt(area / 3.14) * 2;
	disp = getResult("X", 1) - getResult("X", 2) + getResult("X", 4) - getResult("X", 3);
	rp = disp / 0.349;
	ans = Array.concat(d2s(diameter,2), d2s(rp,2));
	Array.print(ans);
}
