function M = rot2d(degrees)

M = [ cosd(degrees) -sind(degrees);
      sind(degrees) cosd(degrees)
    ];

end