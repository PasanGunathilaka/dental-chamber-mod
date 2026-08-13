import { Routes, Route } from 'react-router-dom';
import Typography from '@mui/material/Typography';
import { RegisterPatientPage } from './features/patients/RegisterPatientPage';

function HomePage() {
  return <Typography variant="h4">Dental Management</Typography>;
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/patients/new" element={<RegisterPatientPage />} />
    </Routes>
  );
}

export default App;
