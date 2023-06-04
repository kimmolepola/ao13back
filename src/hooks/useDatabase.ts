// import { useEffect, useCallback } from "react";
// import sqlite3 from "sqlite3";

// const { DB_URL } = process.env;

// let db: sqlite3.Database | undefined;

// export const useDatabase = () => {
//   useEffect(() => {
//     sqlite3.verbose();
//   }, []);

//   const connect = useCallback(() => {
//     db = new sqlite3.Database("./db/asdf.db", (err) => {
//       if (err) {
//         console.error(err.message);
//       }
//       console.log("Connected to the asdf database.");
//     });
//   }, []);

//   const close = useCallback(() => {
//     db?.close((err) => {
//       if (err) {
//         console.error(err.message);
//       }
//       console.log("Close the database connection.");
//     });
//   }, []);

//   const serialize = useCallback(() => {
//     db?.serialize(() => {
//       db?.each(
//         `SELECT PlaylistId as id,
//          Name as name
//          FROM playlists`,
//         (err, row: any) => {
//           if (err) {
//             console.error(err.message);
//           }
//           console.log(row.id + "\t" + row.name);
//         }
//       );
//     });
//   }, []);

//   return { connect, close, serialize };
// };
